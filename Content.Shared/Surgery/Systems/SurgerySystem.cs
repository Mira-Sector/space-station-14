using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage.DamageSelector;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
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

    private readonly Queue<EntityUid> _woundUpdateQueue = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundBodyComponent, InteractUsingEvent>(OnAfterInteract);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        while (_woundUpdateQueue.TryDequeue(out var limb))
        {
            // Ensure the entity exists and has a Construction component.
            if (!TryComp<WoundRecieverComponent>(limb, out var wound))
                continue;

#if EXCEPTION_TOLERANCE
            try
            {
#endif
            // Handle all queued interactions!
            while (wound.InteractionQueue.TryDequeue(out var dequed))
            {
                var (body, interaction) = dequed;
                if (wound.Deleted)
                {
                    Log.Error($"Wound component was deleted while still processing interactions." +
                                $"Entity {ToPrettyString(limb)}, graph: {wound.Graph}, " +
                                $"Body: {ToPrettyString(body)}, " +
                                $"Remaining Queue: {string.Join(", ", wound.InteractionQueue.Select(x => x.GetType().Name))}");
                    break;
                }

                // We set validation to false because we actually want to perform the interaction here.
                HandleEvent(body, limb, interaction, false, wound);
                Dirty(limb, wound);
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

            if (!TryComp<WoundRecieverComponent>(part, out var wound))
                continue;

            if (!HandleEvent(uid, part, args, true, wound))
                return;

            wound.InteractionQueue.Enqueue((uid, args));
            _woundUpdateQueue.Enqueue(part);
            Dirty(part, wound);

            args.Handled = true;
            break;
        }
    }

    private bool HandleEvent(EntityUid uid, EntityUid limb, object ev, bool validation, WoundRecieverComponent? wound = null)
    {
        if (!Resolve(limb, ref wound))
            return false;

        if (GetCurrentNode(limb, wound) is not {} node)
            return false;

        if (GetCurrentEdge(limb, wound) is {} edge)
        {
            var result = HandleEdge(uid, limb, ev, edge, validation, wound);

            if (!validation && result == false && wound.StepIndex == 0)
                wound.EdgeIndex = null;

            return result;
        }

        return HandleNode(uid, limb, ev, node, validation, wound);
    }

    private bool HandleNode(EntityUid uid, EntityUid limb, object ev, SurgeryGraphNode node, bool validation, WoundRecieverComponent? wound = null)
    {
        if (!Resolve(limb, ref wound))
            return false;

        // Let's make extra sure this is zero...
        wound.StepIndex = 0;

        // When we handle a node, we're essentially testing the current event interaction against all of this node's
        // edges' first steps. If any of them accepts the interaction, we stop iterating and enter that edge.
        for (var i = 0; i < node.Edges.Count; i++)
        {
            var edge = node.Edges[i];
            if (HandleEdge(uid, limb, ev, edge, validation, wound) is var result and not false)
            {
                // Only a True result may modify the state.
                // In the case of DoAfter, it's only allowed to modify the waiting flag and the current edge index.
                // In the case of validated, it should NEVER modify the state at all.
                if (!result)
                    return result;

                // If we're not on the same edge as we were before, that means handling that edge changed the node.
                if (wound.Node != node.Name)
                    return result;

                // If we're still in the same node, that means we entered the edge and it's still not done.
                wound.EdgeIndex = i;
                UpdatePathfinding(limb, wound);
                return result;
            }
        }
        return false;
    }

    private bool HandleEdge(EntityUid uid, EntityUid limb, object ev, SurgeryGraphEdge edge, bool validation, WoundRecieverComponent? wound = null)
    {
        if (!Resolve(limb, ref wound))
            return false;

        var step = GetStepFromEdge(edge, wound.StepIndex);
        if (step == null)
        {
            Log.Warning($"Called {nameof(HandleEdge)} on entity {ToPrettyString(uid)} but the current state is not valid for that!");
            return false;
        }

        var handle = HandleStep(uid, limb, ev, step, validation, out var user, wound);
        if (handle != true)
            return handle;

        wound.StepIndex++;

        if (wound.StepIndex >= edge.Steps.Count)
        {
            // Edge finished!
            PerformActions(uid, limb, user, edge.Completed);
            if (wound.Deleted)
                return true;

            wound.TargetEdgeIndex = null;
            wound.EdgeIndex = null;
            wound.StepIndex = 0;

            ChangeNode(uid, limb, user, edge.Target, true, wound);
        }

        return true;
    }

    private bool HandleStep(EntityUid uid, EntityUid limb, object ev, SurgeryGraphStep step, bool validation, out EntityUid? user, WoundRecieverComponent? wound = null)
    {
        user = null;

        if (!Resolve(limb, ref wound))
            return false;

        var handle = HandleInteraction(uid, limb, ev, step, validation, out user, wound);
        if (handle != true)
            return handle;

        PerformActions(uid, limb, user, step.Completed);
        UpdatePathfinding(limb, wound);

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

    private bool HandleInteraction(EntityUid uid, EntityUid limb, object ev, SurgeryGraphStep step, bool validation, out EntityUid? user, WoundRecieverComponent? wound = null)
    {
        user = null;

        if (!Resolve(limb, ref wound))
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
    public bool UpdatePathfinding(EntityUid uid, WoundRecieverComponent? wound = null)
    {
        if (!Resolve(uid, ref wound))
            return false;

        if (wound.TargetNode is not {} targetNodeId)
            return false;

        if (GetCurrentGraph(uid, wound) is not {} graph
            || GetNodeFromGraph(graph, wound.Node) is not {} node
            || GetNodeFromGraph(graph, targetNodeId) is not {} targetNode)
            return false;

        return UpdatePathfinding(uid, graph, node, targetNode, GetCurrentEdge(uid, wound), wound);
    }

    private bool UpdatePathfinding(EntityUid uid, SurgeryPrototype graph,
        SurgeryGraphNode currentNode, SurgeryGraphNode targetNode, SurgeryGraphEdge? currentEdge,
        WoundRecieverComponent? wound = null)
    {
        if (!Resolve(uid, ref wound))
            return false;

        wound.TargetNode = targetNode.Name;

        // Check if we reached the target node.
        if (currentNode == targetNode)
        {
            ClearPathfinding(uid, wound);
            return true;
        }

        // If we don't have a path, generate it.
        if (wound.NodePathfinding == null)
        {
            var path = graph.PathId(currentNode.Name, targetNode.Name);
            if (path == null || path.Length == 0)
            {
                // No path.
                ClearPathfinding(uid, wound);
                return false;
            }

            wound.NodePathfinding = new Queue<string>(path);
        }
        // If the next pathfinding node is the one we're at, dequeue it.
        if (wound.NodePathfinding.Peek() == currentNode.Name)
        {
            wound.NodePathfinding.Dequeue();
        }
        if (currentEdge != null && wound.TargetEdgeIndex is {} targetEdgeIndex)
        {
            if (currentNode.Edges.Count >= targetEdgeIndex)
            {
                // Target edge is incorrect.
                wound.TargetEdgeIndex = null;
            }
            else if (currentNode.Edges[targetEdgeIndex] != currentEdge)
            {
                // We went the wrong way, clean up!
                ClearPathfinding(uid, wound);
                return false;
            }
        }
        if (wound.EdgeIndex == null
            && wound.TargetEdgeIndex == null
            && wound.NodePathfinding != null)
            wound.TargetEdgeIndex = (currentNode.GetEdgeIndex(wound.NodePathfinding.Peek()));
        return true;
    }

    public void ClearPathfinding(EntityUid uid, WoundRecieverComponent? wound = null)
    {
        if (!Resolve(uid, ref wound))
            return;

        wound.TargetNode = null;
        wound.TargetEdgeIndex = null;
        wound.NodePathfinding = null;
    }

    public SurgeryPrototype? GetCurrentGraph(EntityUid uid, WoundRecieverComponent? wound = null)
    {
        if (!Resolve(uid, ref wound, false))
            return null;

        return _protoManager.TryIndex(wound.Graph, out SurgeryPrototype? graph) ? graph : null;
    }

    public SurgeryGraphNode? GetCurrentNode(EntityUid uid, WoundRecieverComponent? wound = null)
    {
        if (!Resolve(uid, ref wound, false))
            return null;

        if (wound.Node is not {} nodeIdentifier)
            return null;

        return GetCurrentGraph(uid, wound) is not {} graph ? null : GetNodeFromGraph(graph, nodeIdentifier);
    }

    public SurgeryGraphEdge? GetCurrentEdge(EntityUid uid, WoundRecieverComponent? wound = null)
    {
        if (!Resolve(uid, ref wound, false))
            return null;

        if (wound.EdgeIndex is not {} edgeIndex)
            return null;

        return GetCurrentNode(uid, wound) is not {} node ? null : GetEdgeFromNode(node, edgeIndex);
    }

    public (SurgeryGraphNode?, SurgeryGraphEdge?) GetCurrentNodeAndEdge(EntityUid uid, WoundRecieverComponent? wound = null)
    {
        if (!Resolve(uid, ref wound, false))
            return (null, null);

        if (GetCurrentNode(uid, wound) is not { } node)
            return (null, null);

        if (wound.EdgeIndex is not {} edgeIndex)
            return (node, null);

        return (node, GetEdgeFromNode(node, edgeIndex));
    }

    public SurgeryGraphStep? GetCurrentStep(EntityUid uid, WoundRecieverComponent? wound = null)
    {
        if (!Resolve(uid, ref wound, false))
            return null;

        if (GetCurrentEdge(uid, wound) is not {} edge)
            return null;

        return GetStepFromEdge(edge, wound.StepIndex);
    }

    public SurgeryGraphNode? GetTargetNode(EntityUid uid, WoundRecieverComponent? wound)
    {
        if (!Resolve(uid, ref wound))
            return null;

        if (wound.TargetNode is not {} targetNodeId)
            return null;

        if (GetCurrentGraph(uid, wound) is not {} graph)
            return null;

        return GetNodeFromGraph(graph, targetNodeId);
    }

    public SurgeryGraphEdge? GetTargetEdge(EntityUid uid, WoundRecieverComponent? wound)
    {
        if (!Resolve(uid, ref wound))
            return null;

        if (wound.TargetEdgeIndex is not {} targetEdgeIndex)
            return null;

        if (GetCurrentNode(uid, wound) is not {} node)
            return null;

        return GetEdgeFromNode(node, targetEdgeIndex);
    }

    public (SurgeryGraphEdge? edge, SurgeryGraphStep? step) GetCurrentEdgeAndStep(EntityUid uid, WoundRecieverComponent? wound = null)
    {
        if (!Resolve(uid, ref wound, false))
            return default;

        var edge = GetCurrentEdge(uid, wound);
        if (edge == null)
            return default;

        var step = GetStepFromEdge(edge, wound.StepIndex);
        return (edge, step);
    }

    public SurgeryGraphNode? GetNodeFromGraph(SurgeryPrototype graph, string id)
    {
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

    public bool ChangeNode(EntityUid uid, EntityUid limb, EntityUid? userUid, string id, bool performActions = true, WoundRecieverComponent? wound = null)
    {
        if (!Resolve(limb, ref wound))
            return false;

        if (GetCurrentGraph(limb, wound) is not {} graph || GetNodeFromGraph(graph, id) is not {} node)
            return false;

        var oldNode = wound.Node;
        wound.Node = id;

        // ChangeEntity will handle the pathfinding update.
        if(performActions)
            PerformActions(uid, limb, userUid, node.Actions);

        // An action might have deleted the entity... Account for this.
        if (!Exists(uid))
            return false;

        UpdatePathfinding(limb, wound);
        return true;
    }
}
