using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Layerable;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Piping.EntitySystems;

public sealed partial class PipeLayerableSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeLayerableComponent, GetVerbsEvent<InteractionVerb>>(OnInteractionVerbs);
        SubscribeLocalEvent<PipeLayerableComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerbs);

        SubscribeLocalEvent<PipeLayerableComponent, InteractHandEvent>(OnHandInteract);
        SubscribeLocalEvent<PipeLayerableComponent, UseInHandEvent>(OnHandUse);
    }

    private void OnInteractionVerbs(Entity<PipeLayerableComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        var verb = new InteractionVerb()
        {
            Text = Loc.GetString("pipe-layerable-increment"),
            Act = () => TryIncrementLayer((ent.Owner, ent.Comp))
        };

        args.Verbs.Add(verb);
    }

    private void OnAlternativeVerbs(Entity<PipeLayerableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        var verb = new AlternativeVerb()
        {
            Text = Loc.GetString("pipe-layerable-decrement"),
            Act = () => TryIncrementLayer((ent.Owner, ent.Comp), true)
        };

        args.Verbs.Add(verb);
    }

    private void OnHandInteract(Entity<PipeLayerableComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!Transform(ent).Anchored)
            return;

        args.Handled = TryIncrementLayer((ent.Owner, ent.Comp));
    }

    private void OnHandUse(Entity<PipeLayerableComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryIncrementLayer((ent.Owner, ent.Comp));
    }

    public bool TryIncrementLayer(Entity<PipeLayerableComponent?> ent, bool reverse = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var newLayer = reverse ? ent.Comp.Layer - 1 : ent.Comp.Layer + 1;

        if (!InBounds((ent.Owner, ent.Comp), newLayer))
            newLayer = reverse ? ent.Comp.MaxLayer : ent.Comp.MinLayer; // wrap around

        return TrySetLayer(ent, newLayer);
    }

    public bool TrySetLayer(Entity<PipeLayerableComponent?> ent, int layer)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var offset = layer - ent.Comp.Layer;
        if (!IsInBounds((ent.Owner, ent.Comp), offset, out var nodes))
            return false;

        foreach (var node in nodes)
        {
            node.Layer += offset;
            _nodeGroup.QueueNodeRemove(node);
            _nodeGroup.QueueReflood(node);
        }

        ent.Comp.Layer = layer;
        _appearance.SetData(ent, PipeLayerVisuals.Layer, layer);
        Dirty(ent);
        return true;
    }

    private bool IsInBounds(Entity<PipeLayerableComponent, NodeContainerComponent?> ent, int offset, out HashSet<PipeNode> nodes)
    {
        nodes = new();

        if (!Resolve(ent, ref ent.Comp2))
            return false;

        HashSet<int> otherEntityLayers = new();

        var xform = Transform(ent);

        if (xform.GridUid is {} gridUid && TryComp<MapGridComponent>(gridUid, out var grid))
        {
            foreach (var anchoredEnt in _map.GetAnchoredEntities(gridUid, grid, xform.Coordinates))
            {
                if (!TryComp<NodeContainerComponent>(anchoredEnt, out var anchoredEntNode))
                    continue;

                foreach (var node in anchoredEntNode.Nodes.Values)
                {
                    if (node is not PipeNode pipeNode)
                        continue;

                    otherEntityLayers.Add(pipeNode.Layer);
                }
            }
        }

        foreach (var node in ent.Comp2.Nodes.Values)
        {
            if (node is not PipeNode pipeNode)
                continue;

            nodes.Add(pipeNode);

            var newLayer = pipeNode.Layer + offset;
            if (!InBounds(ent, newLayer))
                return false;

            if (otherEntityLayers.Contains(newLayer))
                return false;
        }

        return true;
    }

    private bool InBounds(Entity<PipeLayerableComponent> ent, int layer)
    {
        return ent.Comp.MinLayer <= layer && layer <= ent.Comp.MaxLayer;
    }
}
