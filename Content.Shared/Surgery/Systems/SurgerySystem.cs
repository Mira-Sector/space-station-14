using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage.DamageSelector;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Surgery.Components;
using Content.Shared.Surgery.Prototypes;
using Content.Shared.Surgery.Steps;
using Content.Shared.Tools.Systems;
using Content.Shared.Wounds.Components;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Surgery.Systems;

public sealed partial class SurgerySystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    private readonly Queue<EntityUid> _surgeryUpdateQueue = new();

    public override void Initialize()
    {
        base.Initialize();

        GraphInit();

        SubscribeLocalEvent<WoundBodyComponent, InteractUsingEvent>(OnAfterInteract);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        while (_surgeryUpdateQueue.TryDequeue(out var limb))
        {
            // Ensure the entity exists and has a Construction component.
            if (!TryComp<SurgeryReceiverComponent>(limb, out var surgery))
                continue;

#if EXCEPTION_TOLERANCE
            try
            {
#endif
            // Handle all queued interactions!
            while (surgery.InteractionQueue.TryDequeue(out var dequed))
            {
                var (body, interaction) = dequed;
                if (surgery.Deleted)
                {
                    Log.Error($"surgery component was deleted while still processing interactions." +
                                $"Entity {ToPrettyString(limb)}, graph: {surgery.Graph}, " +
                                $"Body: {ToPrettyString(body)}, " +
                                $"Remaining Queue: {string.Join(", ", surgery.InteractionQueue.Select(x => x.GetType().Name))}");
                    break;
                }

                // We set validation to false because we actually want to perform the interaction here.
                HandleEvent(body, limb, interaction, false, surgery);
                Dirty(limb, surgery);
            }

#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                Log.Error($"Caught exception while processing construction queue. Entity {ToPrettyString(uid)}, graph: {construction.Graph}");
                _runtimeLog.LogException(e, $"{nameof(ConstructionSystem)}.{nameof(UpdateInteractions)}");
                Del(uid);
            }
#endif
        }
    }

    private void OnAfterInteract(EntityUid uid, WoundBodyComponent component, InteractUsingEvent args)
    {
        if (args.Handled || component.Limbs.Count <= 0)
            return;

        if (!TryComp<BodyComponent>(uid, out var bodyComp))
            return;

        if (!TryComp<DamagePartSelectorComponent>(args.User, out var selectorComp))
            return;

        // find the wounded part if it exists
        foreach (var part in component.Limbs)
        {
            if (!TryComp<BodyPartComponent>(part, out var partComp))
                continue;

            if (partComp.PartType != selectorComp.SelectedPart.Type)
                continue;

            if (partComp.Symmetry != selectorComp.SelectedPart.Side)
                continue;

            if (!TryComp<SurgeryReceiverComponent>(part, out var surgery))
                continue;

            if (!HandleEvent(uid, part, args, true, surgery))
                return;

            surgery.InteractionQueue.Enqueue((uid, args));
            _surgeryUpdateQueue.Enqueue(part);
            Dirty(part, surgery);

            args.Handled = true;
            break;
        }
    }

    private bool HandleEvent(EntityUid uid, EntityUid limb, object ev, bool validation, SurgeryReceiverComponent? surgery = null)
    {
        if (!Resolve(limb, ref surgery))
            return false;

        if (GetCurrentNode(limb, surgery) is not {} node)
            return false;

        if (GetCurrentEdge(limb, surgery) is {} edge)
        {
            var result = HandleEdge(uid, limb, ev, edge, validation, surgery);

            if (!validation && result == false && surgery.StepIndex == 0)
                surgery.EdgeIndex = null;

            return result;
        }

        return HandleNode(uid, limb, ev, node, validation, surgery);
    }

    private bool HandleNode(EntityUid uid, EntityUid limb, object ev, SurgeryGraphNode node, bool validation, SurgeryReceiverComponent? surgery = null)
    {
        if (!Resolve(limb, ref surgery))
            return false;

        // Let's make extra sure this is zero...
        surgery.StepIndex = 0;

        // When we handle a node, we're essentially testing the current event interaction against all of this node's
        // edges' first steps. If any of them accepts the interaction, we stop iterating and enter that edge.
        for (var i = 0; i < node.Edges.Count; i++)
        {
            var edge = node.Edges[i];
            if (HandleEdge(uid, limb, ev, edge, validation, surgery) is var result and not false)
            {
                // Only a True result may modify the state.
                // In the case of DoAfter, it's only allowed to modify the waiting flag and the current edge index.
                // In the case of validated, it should NEVER modify the state at all.
                if (!result)
                    return result;

                // If we're not on the same edge as we were before, that means handling that edge changed the node.
                if (surgery.Node != node.Name)
                    return result;

                // If we're still in the same node, that means we entered the edge and it's still not done.
                surgery.EdgeIndex = i;
                UpdatePathfinding(limb, surgery);
                return result;
            }
        }
        return false;
    }

    private bool HandleEdge(EntityUid uid, EntityUid limb, object ev, SurgeryGraphEdge edge, bool validation, SurgeryReceiverComponent? surgery = null)
    {
        if (!Resolve(limb, ref surgery))
            return false;

        var step = GetStepFromEdge(edge, surgery.StepIndex);
        if (step == null)
        {
            Log.Warning($"Called {nameof(HandleEdge)} on entity {ToPrettyString(uid)} but the current state is not valid for that!");
            return false;
        }

        var handle = HandleStep(uid, limb, ev, step, validation, out var user, surgery);
        if (handle != true)
            return handle;

        surgery.StepIndex++;

        if (surgery.StepIndex >= edge.Steps.Count)
        {
            // Edge finished!
            PerformActions(uid, limb, user, edge.Completed);
            if (surgery.Deleted)
                return true;

            surgery.TargetEdgeIndex = null;
            surgery.EdgeIndex = null;
            surgery.StepIndex = 0;

            ChangeNode(uid, limb, user, edge.Target, true, surgery);
        }

        return true;
    }

    private bool HandleStep(EntityUid uid, EntityUid limb, object ev, SurgeryGraphStep step, bool validation, out EntityUid? user, SurgeryReceiverComponent? surgery = null)
    {
        user = null;

        if (!Resolve(limb, ref surgery))
            return false;

        var handle = HandleInteraction(uid, limb, ev, step, validation, out user, surgery);
        if (handle != true)
            return handle;

        PerformActions(uid, limb, user, step.Completed);
        UpdatePathfinding(limb, surgery);

        return true;
    }

    public void PerformActions(EntityUid bodyUid, EntityUid limbUid, EntityUid? userUid, IEnumerable<ISurgeryAction> actions, BodyComponent? bodyComp = null)
    {
        if (!Resolve(bodyUid, ref bodyComp))
            return;

        foreach (var action in actions)
        {
            if (!Exists(bodyUid) || !Exists(limbUid))
                break;

            action.PerformAction(bodyUid, limbUid, userUid, bodyComp, EntityManager);
        }
    }

    private bool HandleInteraction(EntityUid uid, EntityUid limb, object ev, SurgeryGraphStep step, bool validation, out EntityUid? user, SurgeryReceiverComponent? surgery = null)
    {
        user = null;

        if (!Resolve(limb, ref surgery))
            return false;

        switch (step)
        {
            case ToolSurgeryGraphStep toolStep:
            {
                if (ev is not InteractUsingEvent interactUsing)
                    break;

                user = interactUsing.User;

                // If we're validating whether this event handles the step...
                if (validation)
                    return _tool.HasQuality(interactUsing.Used, toolStep.Tool);

                var result  = _tool.UseTool(
                    interactUsing.Used,
                    interactUsing.User,
                    uid,
                    TimeSpan.FromSeconds(toolStep.DoAfter),
                    new [] { toolStep.Tool },
                    new SurgeryInteractionDoAfterEvent(),
                    out var doAfter,
                    toolStep.Fuel);

                return result && doAfter != null;
            }

        }

        return false;
    }
    public bool UpdatePathfinding(EntityUid uid, SurgeryReceiverComponent? surgery = null)
    {
        if (!Resolve(uid, ref surgery))
            return false;

        if (surgery.TargetNode is not {} targetNodeId)
            return false;

        if (GetNodeFromGraph(surgery.Graph, surgery.Node) is not {} node
            || GetNodeFromGraph(surgery.Graph, targetNodeId) is not {} targetNode)
            return false;

        return UpdatePathfinding(uid, surgery.Graph, node, targetNode, GetCurrentEdge(uid, surgery), surgery);
    }

    private bool UpdatePathfinding(EntityUid uid, SurgeryGraph graph,
        SurgeryGraphNode currentNode, SurgeryGraphNode targetNode, SurgeryGraphEdge? currentEdge,
        SurgeryReceiverComponent? surgery = null)
    {
        if (!Resolve(uid, ref surgery))
            return false;

        surgery.TargetNode = targetNode.Name;

        // Check if we reached the target node.
        if (currentNode == targetNode)
        {
            ClearPathfinding(uid, surgery);
            return true;
        }

        // If we don't have a path, generate it.
        if (surgery.NodePathfinding == null)
        {
            var path = graph.PathId(currentNode.Name, targetNode.Name);
            if (path == null || path.Length == 0)
            {
                // No path.
                ClearPathfinding(uid, surgery);
                return false;
            }

            surgery.NodePathfinding = new Queue<string>(path);
        }
        // If the next pathfinding node is the one we're at, dequeue it.
        if (surgery.NodePathfinding.Peek() == currentNode.Name)
        {
            surgery.NodePathfinding.Dequeue();
        }
        if (currentEdge != null && surgery.TargetEdgeIndex is {} targetEdgeIndex)
        {
            if (currentNode.Edges.Count >= targetEdgeIndex)
            {
                // Target edge is incorrect.
                surgery.TargetEdgeIndex = null;
            }
            else if (currentNode.Edges[targetEdgeIndex] != currentEdge)
            {
                // We went the wrong way, clean up!
                ClearPathfinding(uid, surgery);
                return false;
            }
        }
        if (surgery.EdgeIndex == null
            && surgery.TargetEdgeIndex == null
            && surgery.NodePathfinding != null)
            surgery.TargetEdgeIndex = (currentNode.GetEdgeIndex(surgery.NodePathfinding.Peek()));
        return true;
    }

    public void ClearPathfinding(EntityUid uid, SurgeryReceiverComponent? surgery = null)
    {
        if (!Resolve(uid, ref surgery))
            return;

        surgery.TargetNode = null;
        surgery.TargetEdgeIndex = null;
        surgery.NodePathfinding = null;
    }

    public SurgeryGraphNode? GetCurrentNode(EntityUid uid, SurgeryReceiverComponent? surgery = null)
    {
        if (!Resolve(uid, ref surgery, false))
            return null;

        if (surgery.Node is not {} nodeIdentifier)
            return null;

        return GetNodeFromGraph(surgery.Graph, nodeIdentifier);
    }

    public SurgeryGraphEdge? GetCurrentEdge(EntityUid uid, SurgeryReceiverComponent? surgery = null)
    {
        if (!Resolve(uid, ref surgery, false))
            return null;

        if (surgery.EdgeIndex is not {} edgeIndex)
            return null;

        return GetCurrentNode(uid, surgery) is not {} node ? null : GetEdgeFromNode(node, edgeIndex);
    }

    public (SurgeryGraphNode?, SurgeryGraphEdge?) GetCurrentNodeAndEdge(EntityUid uid, SurgeryReceiverComponent? surgery = null)
    {
        if (!Resolve(uid, ref surgery, false))
            return (null, null);

        if (GetCurrentNode(uid, surgery) is not { } node)
            return (null, null);

        if (surgery.EdgeIndex is not {} edgeIndex)
            return (node, null);

        return (node, GetEdgeFromNode(node, edgeIndex));
    }

    public SurgeryGraphStep? GetCurrentStep(EntityUid uid, SurgeryReceiverComponent? surgery = null)
    {
        if (!Resolve(uid, ref surgery, false))
            return null;

        if (GetCurrentEdge(uid, surgery) is not {} edge)
            return null;

        return GetStepFromEdge(edge, surgery.StepIndex);
    }

    public SurgeryGraphNode? GetTargetNode(EntityUid uid, SurgeryReceiverComponent? surgery)
    {
        if (!Resolve(uid, ref surgery))
            return null;

        if (surgery.TargetNode is not {} targetNodeId)
            return null;

        return GetNodeFromGraph(surgery.Graph, targetNodeId);
    }

    public SurgeryGraphEdge? GetTargetEdge(EntityUid uid, SurgeryReceiverComponent? surgery)
    {
        if (!Resolve(uid, ref surgery))
            return null;

        if (surgery.TargetEdgeIndex is not {} targetEdgeIndex)
            return null;

        if (GetCurrentNode(uid, surgery) is not {} node)
            return null;

        return GetEdgeFromNode(node, targetEdgeIndex);
    }

    public (SurgeryGraphEdge? edge, SurgeryGraphStep? step) GetCurrentEdgeAndStep(EntityUid uid, SurgeryReceiverComponent? surgery = null)
    {
        if (!Resolve(uid, ref surgery, false))
            return default;

        var edge = GetCurrentEdge(uid, surgery);
        if (edge == null)
            return default;

        var step = GetStepFromEdge(edge, surgery.StepIndex);
        return (edge, step);
    }

    public SurgeryGraphNode? GetNodeFromGraph(SurgeryGraph graph, string? id)
    {
        if (id == null)
            return null;

        return graph.Nodes.TryGetValue(id, out var node) ? node : null;
    }

    public SurgeryGraphEdge? GetEdgeFromNode(SurgeryGraphNode node, int index)
    {
        return node.Edges.Count > index ? node.Edges[index] : null;
    }

    public SurgeryGraphStep? GetStepFromEdge(SurgeryGraphEdge edge, int index)
    {
        return edge.Steps.Count > index ? edge.Steps[index] : null;
    }

    public bool ChangeNode(EntityUid uid, EntityUid limb, EntityUid? userUid, string id, bool performActions = true, SurgeryReceiverComponent? surgery = null)
    {
        if (!Resolve(limb, ref surgery))
            return false;

        if (GetNodeFromGraph(surgery.Graph, id) is not {} node)
            return false;

        var oldNode = surgery.Node;
        surgery.Node = id;

        // ChangeEntity will handle the pathfinding update.
        if(performActions)
            PerformActions(uid, limb, userUid, node.Actions);

        // An action might have deleted the entity... Account for this.
        if (!Exists(uid))
            return false;

        UpdatePathfinding(limb, surgery);
        return true;
    }
}
