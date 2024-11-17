using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.DamageSelector;
using Content.Shared.Interaction;
using Content.Shared.Wounds.Components;
using Content.Shared.Wounds.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Wounds.Systems;

public sealed partial class WoundSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundComponent, ComponentInit>(OnWoundInit);

        SubscribeLocalEvent<WoundBodyComponent, DamageModifyEvent>(OnDamage);
        SubscribeLocalEvent<WoundBodyComponent, InteractUsingEvent>(OnAfterInteract);
    }

    private void OnWoundInit(EntityUid uid, WoundComponent component, ComponentInit args)
    {
    }

    private void OnDamage(EntityUid uid, WoundBodyComponent component, DamageModifyEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        if (!TryComp<BodyComponent>(uid, out var bodyComp))
            return;

        if (!TryComp<DamagePartSelectorComponent>(args.Origin, out var selectorComp))
            return;

        var parts = _body.GetBodyChildren(uid, bodyComp);

        foreach (var (partUid, partComp) in parts)
        {
            if (partComp.PartType != selectorComp.SelectedPart.Type)
                continue;

            if (partComp.Symmetry != selectorComp.SelectedPart.Side)
                continue;

            if (!TryComp<WoundRecieverComponent>(partUid, out var woundRecieverComp))
                continue;

            // only one wound per limb
            if (HasComp<WoundComponent>(partUid))
                return;

            // TODO: select based on damage and other factors
            var woundId = _random.Pick(woundRecieverComp.SelectableWounds);

            if (!_protoManager.TryIndex(woundId, out WoundPrototype? wound))
            {
                Log.Debug($"{woundId} is not a valid wound prototype.");
                return;
            }

            if (string.IsNullOrEmpty(wound.Start))
                return;

            var woundComp = EnsureComp<WoundComponent>(partUid);
            component.Limbs.Add(partUid);
            Dirty(uid, component);

            woundComp.Graph = woundId;
            woundComp.Node = wound.Start;
            Dirty(partUid, woundComp);

            return;
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

        EntityUid? limb = null;
        WoundComponent? wound = null;

        // find the wounded part if it exists
        foreach (var part in component.Limbs)
        {
            if (!TryComp<BodyPartComponent>(part, out var partComp))
                continue;

            if (partComp.PartType != selectorComp.SelectedPart.Type)
                continue;

            if (partComp.Symmetry != selectorComp.SelectedPart.Side)
                continue;

            if (!TryComp<WoundComponent>(part, out var partWoundComp))
                continue;

            limb = part;
            wound = partWoundComp;
            break;
        }

        if (wound == null || limb == null)
            return;

        if (!HandleEvent(uid, limb.Value, true, wound))
            return;

        wound.InteractionQueue.Enqueue(args);

        args.Handled = true;
    }

    private bool HandleEvent(EntityUid uid, EntityUid limb, bool validation, WoundComponent? wound = null)
    {
        if (!Resolve(uid, ref wound))
            return false;

        if (GetCurrentNode(uid, wound) is not {} node)
            return false;

        if (GetCurrentEdge(uid, wound) is {} edge)
        {
            var result = HandleEdge(uid, limb, edge, validation, wound);

            if (!validation && result == false && wound.StepIndex == 0)
                wound.EdgeIndex = null;

            return result;
        }

        return HandleNode(uid, limb, node, validation, wound);
    }

    private bool HandleNode(EntityUid uid, EntityUid limb, WoundGraphNode node, bool validation, WoundComponent? wound = null)
    {
        if (!Resolve(uid, ref wound))
            return false;

        // Let's make extra sure this is zero...
        wound.StepIndex = 0;

        // When we handle a node, we're essentially testing the current event interaction against all of this node's
        // edges' first steps. If any of them accepts the interaction, we stop iterating and enter that edge.
        for (var i = 0; i < node.Edges.Count; i++)
        {
            var edge = node.Edges[i];
            if (HandleEdge(uid, limb, edge, validation, wound) is var result and not false)
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
                UpdatePathfinding(uid, wound);
                return result;
            }
        }
        return false;
    }

    private bool HandleEdge(EntityUid uid, EntityUid limb, WoundGraphEdge edge, bool validation, WoundComponent? wound = null)
    {
        if (!Resolve(uid, ref wound))
            return false;

        var step = GetStepFromEdge(edge, wound.StepIndex);
        if (step == null)
        {
            Log.Warning($"Called {nameof(HandleEdge)} on entity {ToPrettyString(uid)} but the current state is not valid for that!");
            return false;
        }

        var handle = HandleStep(uid, limb, step, validation, out var user, wound);
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

    private bool HandleStep(EntityUid uid, EntityUid limb, WoundGraphStep step, bool validation, out EntityUid? user, WoundComponent? wound = null)
    {
        user = null;

        if (!Resolve(uid, ref wound))
            return false;

        var handle = HandleInteraction(uid, step, validation, out user, wound);
        if (handle != true)
            return handle;

        PerformActions(uid, limb, user, step.Completed);
        UpdatePathfinding(uid, wound);

        return true;
    }

    public void PerformActions(EntityUid bodyUid, EntityUid limbUid, EntityUid? userUid, IEnumerable<IWoundAction> actions, BodyComponent? bodyComp = null)
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

    private bool HandleInteraction(EntityUid uid, WoundGraphStep step, bool validation, out EntityUid? user, WoundComponent? wound = null)
    {
        user = null;
        if (!Resolve(uid, ref wound))
            return false;

        switch (step)
        {
        }

        return false;
    }
    public bool UpdatePathfinding(EntityUid uid, WoundComponent? wound = null)
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

    private bool UpdatePathfinding(EntityUid uid, WoundPrototype graph,
        WoundGraphNode currentNode, WoundGraphNode targetNode, WoundGraphEdge? currentEdge,
        WoundComponent? wound = null)
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

    public void ClearPathfinding(EntityUid uid, WoundComponent? wound = null)
    {
        if (!Resolve(uid, ref wound))
            return;

        wound.TargetNode = null;
        wound.TargetEdgeIndex = null;
        wound.NodePathfinding = null;
    }

    public WoundPrototype? GetCurrentGraph(EntityUid uid, WoundComponent? wound = null)
    {
        if (!Resolve(uid, ref wound, false))
            return null;

        return _protoManager.TryIndex(wound.Graph, out WoundPrototype? graph) ? graph : null;
    }

    public WoundGraphNode? GetCurrentNode(EntityUid uid, WoundComponent? wound = null)
    {
        if (!Resolve(uid, ref wound, false))
            return null;

        if (wound.Node is not {} nodeIdentifier)
            return null;

        return GetCurrentGraph(uid, wound) is not {} graph ? null : GetNodeFromGraph(graph, nodeIdentifier);
    }

    public WoundGraphEdge? GetCurrentEdge(EntityUid uid, WoundComponent? wound = null)
    {
        if (!Resolve(uid, ref wound, false))
            return null;

        if (wound.EdgeIndex is not {} edgeIndex)
            return null;

        return GetCurrentNode(uid, wound) is not {} node ? null : GetEdgeFromNode(node, edgeIndex);
    }

    public (WoundGraphNode?, WoundGraphEdge?) GetCurrentNodeAndEdge(EntityUid uid, WoundComponent? wound = null)
    {
        if (!Resolve(uid, ref wound, false))
            return (null, null);

        if (GetCurrentNode(uid, wound) is not { } node)
            return (null, null);

        if (wound.EdgeIndex is not {} edgeIndex)
            return (node, null);

        return (node, GetEdgeFromNode(node, edgeIndex));
    }

    public WoundGraphStep? GetCurrentStep(EntityUid uid, WoundComponent? wound = null)
    {
        if (!Resolve(uid, ref wound, false))
            return null;

        if (GetCurrentEdge(uid, wound) is not {} edge)
            return null;

        return GetStepFromEdge(edge, wound.StepIndex);
    }

    public WoundGraphNode? GetTargetNode(EntityUid uid, WoundComponent? wound)
    {
        if (!Resolve(uid, ref wound))
            return null;

        if (wound.TargetNode is not {} targetNodeId)
            return null;

        if (GetCurrentGraph(uid, wound) is not {} graph)
            return null;

        return GetNodeFromGraph(graph, targetNodeId);
    }

    public WoundGraphEdge? GetTargetEdge(EntityUid uid, WoundComponent? wound)
    {
        if (!Resolve(uid, ref wound))
            return null;

        if (wound.TargetEdgeIndex is not {} targetEdgeIndex)
            return null;

        if (GetCurrentNode(uid, wound) is not {} node)
            return null;

        return GetEdgeFromNode(node, targetEdgeIndex);
    }

    public (WoundGraphEdge? edge, WoundGraphStep? step) GetCurrentEdgeAndStep(EntityUid uid, WoundComponent? wound = null)
    {
        if (!Resolve(uid, ref wound, false))
            return default;

        var edge = GetCurrentEdge(uid, wound);
        if (edge == null)
            return default;

        var step = GetStepFromEdge(edge, wound.StepIndex);
        return (edge, step);
    }

    public WoundGraphNode? GetNodeFromGraph(WoundPrototype graph, string id)
    {
        return graph.Nodes.TryGetValue(id, out var node) ? node : null;
    }

    public WoundGraphEdge? GetEdgeFromNode(WoundGraphNode node, int index)
    {
        return node.Edges.Count > index ? node.Edges[index] : null;
    }

    public WoundGraphStep? GetStepFromEdge(WoundGraphEdge edge, int index)
    {
        return edge.Steps.Count > index ? edge.Steps[index] : null;
    }

    public bool ChangeNode(EntityUid uid, EntityUid limb, EntityUid? userUid, string id, bool performActions = true, WoundComponent? wound = null)
    {
        if (!Resolve(uid, ref wound))
            return false;

        if (GetCurrentGraph(uid, wound) is not {} graph ||  GetNodeFromGraph(graph, id) is not {} node)
            return false;

        var oldNode = wound.Node;
        wound.Node = id;

        // ChangeEntity will handle the pathfinding update.
        if(performActions)
            PerformActions(uid, limb, userUid, node.Actions);

        // An action might have deleted the entity... Account for this.
        if (!Exists(uid))
            return false;

        UpdatePathfinding(uid, wound);
        return true;
    }
}
