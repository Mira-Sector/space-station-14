using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Atmos.Piping.Crawling.Systems;
using Content.Shared.Maps;
using Content.Shared.NodeContainer;

namespace Content.Server.Atmos.Piping.Crawling;

public sealed partial class PipeCrawlingSystem : SharedPipeCrawlingSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private EntityQuery<NodeContainerComponent> _nodeQuery;

    private static readonly LookupFlags LookupFlags = LookupFlags.Approximate | LookupFlags.Static | LookupFlags.Sundries | LookupFlags.Sensors;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeCrawlingPipeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PipeCrawlingPipeComponent, AnchorStateChangedEvent>(OnAnchor);

        _nodeQuery = GetEntityQuery<NodeContainerComponent>();
    }

    private void OnMapInit(Entity<PipeCrawlingPipeComponent> ent, ref MapInitEvent args)
    {
        UpdatePipeConnections(ent);
    }

    private void OnAnchor(Entity<PipeCrawlingPipeComponent> ent, ref AnchorStateChangedEvent args)
    {
        // prevent running on map init
        // that has its own logic flow to not updating neighbors
        if (!Initialized(ent.Owner))
            return;

        if (args.Anchored)
        {
            UpdatePipeConnections(ent);
        }
        else
        {
            ent.Comp.ConnectedPipes.Clear();
            Dirty(ent);
        }

        // now update anyone who is close by
        foreach (var neighbor in GetNeighbors(ent))
            UpdatePipeConnections(neighbor);
    }

    private void UpdatePipeConnections(Entity<PipeCrawlingPipeComponent> ent)
    {
        var pipeLayers = GetPipeLayers(ent.Owner);
        var pipeTile = Transform(ent.Owner).Coordinates.GetTileRef()?.GridIndices;

        ent.Comp.ConnectedPipes.Clear();

        foreach (var neighbor in GetNeighbors(ent))
        {
            var neighborLayers = GetPipeLayers(neighbor);
            var neighborTile = Transform(neighbor).Coordinates.GetTileRef()?.GridIndices;

            foreach (var (connectedLayer, connectedDirections) in GetPotentialConnections(pipeLayers, neighborLayers))
            {
                if (!ent.Comp.ConnectedPipes.TryGetValue(connectedLayer, out var connections))
                {
                    connections = [];
                    ent.Comp.ConnectedPipes[connectedLayer] = connections;
                }

                foreach (var connectedDirection in connectedDirections)
                {
                    // check the direction points at our neighbor
                    var directionVec = DirectionExtensions.ToIntVec(connectedDirection);
                    if (neighborTile == pipeTile + directionVec)
                        connections[connectedDirection] = GetNetEntity(neighbor);
                }
            }
        }

        Dirty(ent);
    }

    private HashSet<Entity<PipeCrawlingPipeComponent>> GetNeighbors(EntityUid uid)
    {
        HashSet<Entity<PipeCrawlingPipeComponent>> neighbors = [];
        var xform = Transform(uid);

        if (xform.GridUid is not { } gridUid)
            return neighbors;

        var pipePos = xform.LocalPosition;

        foreach (var direction in DirectionExtensions.AllDirections)
        {
            if (!IsCardinalDirection(direction))
                continue;

            var neighborPos = (Vector2i)pipePos + direction.ToIntVec();
            foreach (var neighbor in _lookup.GetLocalEntitiesIntersecting(gridUid, neighborPos, 0f, LookupFlags))
            {
                if (TerminatingOrDeleted(neighbor))
                    continue;

                if (!Transform(neighbor).Anchored)
                    continue;

                if (PipeQuery.TryComp(neighbor, out var neighborPipe))
                    neighbors.Add((neighbor, neighborPipe));
            }
        }

        return neighbors;
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

    private static Dictionary<AtmosPipeLayer, HashSet<Direction>> GetPotentialConnections(Dictionary<AtmosPipeLayer, PipeDirection> mainPipe, Dictionary<AtmosPipeLayer, PipeDirection> otherPipe)
    {
        Dictionary<AtmosPipeLayer, HashSet<Direction>> connections = [];

        foreach (var (layer, mainDirection) in mainPipe)
        {
            if (!otherPipe.TryGetValue(layer, out var otherDirection))
                continue;

            HashSet<Direction> connectionDirections = [];

            for (var i = 0; i < PipeDirectionHelpers.PipeDirections; i++)
            {
                var direction = (PipeDirection)(1 << i);
                if (!mainDirection.HasFlag(direction))
                    continue;

                var oppositeDirection = PipeDirectionHelpers.GetOpposite(direction);
                if (!otherDirection.HasFlag(oppositeDirection))
                    continue;

                connectionDirections.Add(PipeDirectionHelpers.ToDirection(direction));
            }

            connections.Add(layer, connectionDirections);
        }

        return connections;
    }
}
