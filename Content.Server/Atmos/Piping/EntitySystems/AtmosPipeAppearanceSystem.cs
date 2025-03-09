using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Piping.EntitySystems;

public sealed class AtmosPipeAppearanceSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeAppearanceComponent, NodeGroupsRebuilt>(OnNodeUpdate);
    }

    private void OnNodeUpdate(EntityUid uid, PipeAppearanceComponent component, ref NodeGroupsRebuilt args)
    {
        UpdateAppearance(args.NodeOwner, component);
    }

    private void UpdateAppearance(EntityUid uid, PipeAppearanceComponent component, AppearanceComponent? appearance = null, NodeContainerComponent? container = null,
        TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref appearance, ref container, ref xform, false))
            return;

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        // get connected entities
        var anyPipeNodes = false;
        Dictionary<EntityUid, HashSet<int>> connected = new();
        foreach (var node in container.Nodes.Values)
        {
            if (node is not PipeNode)
                continue;

            anyPipeNodes = true;

            foreach (var connectedNode in node.ReachableNodes)
            {
                if (connectedNode is not PipeNode pipeNode)
                    continue;

                if (connected.TryGetValue(pipeNode.Owner, out var layers))
                {
                    layers.Add(pipeNode.Layer);
                }
                else
                {
                    layers = new();
                    layers.Add(pipeNode.Layer);
                    connected.Add(connectedNode.Owner, layers);
                }
            }
        }

        if (!anyPipeNodes)
            return;

        component.ConnectedDirections.Clear();

        // find the cardinal directions of any connected entities
        var tile = grid.TileIndicesFor(xform.Coordinates);
        foreach (var (neighbour, layers) in connected)
        {
            foreach (var layer in layers)
            {
                if (!component.ConnectedDirections.TryGetValue(layer, out var directions))
                {
                    directions = PipeDirection.None;
                    component.ConnectedDirections.Add(layer, directions);
                }

                var otherTile = grid.TileIndicesFor(Transform(neighbour).Coordinates);

                component.ConnectedDirections[layer] |= (otherTile - tile) switch
                {
                    (0, 1) => PipeDirection.North,
                    (0, -1) => PipeDirection.South,
                    (1, 0) => PipeDirection.East,
                    (-1, 0) => PipeDirection.West,
                    _ => PipeDirection.None
                };
            }
        }

        Dirty(uid, component);
        _appearance.QueueUpdate(uid, appearance);
    }
}
