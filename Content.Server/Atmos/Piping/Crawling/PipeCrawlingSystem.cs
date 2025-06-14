using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Atmos.Piping.Crawling.Systems;
using Content.Shared.NodeContainer;

namespace Content.Server.Atmos.Piping.Crawling;

public sealed partial class PipeCrawlingSystem : SharedPipeCrawlingSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private EntityQuery<PipeCrawlingPipeComponent> _pipeQuery;
    private EntityQuery<NodeContainerComponent> _nodeQuery;

    private static readonly LookupFlags LookupFlags = LookupFlags.Approximate | LookupFlags.Static | LookupFlags.Sundries | LookupFlags.Sensors;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeCrawlingPipeComponent, MapInitEvent>(OnMapInit);

        _pipeQuery = GetEntityQuery<PipeCrawlingPipeComponent>();
        _nodeQuery = GetEntityQuery<NodeContainerComponent>();
    }

    private void OnMapInit(Entity<PipeCrawlingPipeComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent.Owner);

        if (xform.GridUid is not { } gridUid)
            return;

        var pipePos = Transform(ent.Owner).LocalPosition;
        var pipeLayers = GetPipeLayers(ent.Owner);

        foreach (var direction in DirectionExtensions.AllDirections)
        {
            if (!IsCardinalDirection(direction))
                continue;

            var neighborPos = (Vector2i)pipePos + direction.ToIntVec();
            var neighbors = _lookup.GetLocalEntitiesIntersecting(gridUid, neighborPos, 0f, LookupFlags);
            foreach (var neighbor in neighbors)
            {
                if (!_pipeQuery.HasComp(neighbor))
                    continue;

                var neighborLayers = GetPipeLayers(neighbor);
                if (GetConnection(pipeLayers, neighborLayers) is not { } connection)
                    continue;

                var (connectedLayer, connectedDirection) = connection;

                if (!ent.Comp.ConnectedPipes.TryGetValue(connectedLayer, out var connections))
                {
                    connections = [];
                    ent.Comp.ConnectedPipes[connectedLayer] = connections;
                }

                connections[connectedDirection] = GetNetEntity(neighbor);
            }
        }
    }

    // why this isnt in engine already baffles me
    private static bool IsCardinalDirection(Direction direction)
    {
        return direction switch
        {
            Direction.North => true,
            Direction.South => true,
            Direction.East => true,
            Direction.West => true,
            _ => false
        };
    }

    private Dictionary<AtmosPipeLayer, PipeDirection> GetPipeLayers(EntityUid uid)
    {
        if (!_nodeQuery.TryComp(uid, out var nodeComp))
            return [];

        Dictionary<AtmosPipeLayer, PipeDirection> layers = [];
        foreach (var node in nodeComp.Nodes.Values)
        {
            if (node is not PipeNode pipeNode)
                continue;

            if (layers.ContainsKey(pipeNode.CurrentPipeLayer))
                layers[pipeNode.CurrentPipeLayer] |= pipeNode.CurrentPipeDirection;
            else
                layers.Add(pipeNode.CurrentPipeLayer, pipeNode.CurrentPipeDirection);
        }

        return layers;
    }

    private static (AtmosPipeLayer, Direction)? GetConnection(Dictionary<AtmosPipeLayer, PipeDirection> mainPipe, Dictionary<AtmosPipeLayer, PipeDirection> otherPipe)
    {
        foreach (var (layer, mainDirection) in mainPipe)
        {
            if (!otherPipe.TryGetValue(layer, out var otherDirection))
                continue;

            for (var i = 0; i < PipeDirectionHelpers.PipeDirections; i++)
            {
                var direction = (PipeDirection)(1 << i);
                if (!mainDirection.HasFlag(direction))
                    continue;

                var oppositeDirection = PipeDirectionHelpers.GetOpposite(direction);
                if (!otherDirection.HasFlag(oppositeDirection))
                    continue;

                return (layer, PipeDirectionHelpers.ToDirection(direction));
            }
        }

        return null;
    }
}
