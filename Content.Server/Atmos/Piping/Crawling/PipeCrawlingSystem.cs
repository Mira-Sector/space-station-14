using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Atmos.Piping.Crawling.Systems;
using Content.Shared.Maps;
using Content.Shared.NodeContainer;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Atmos.Piping.Crawling;

public sealed partial class PipeCrawlingSystem : SharedPipeCrawlingSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InternalsSystem _internals = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private EntityQuery<NodeContainerComponent> _nodeQuery;

    private const LookupFlags Flags = LookupFlags.Approximate | LookupFlags.Static | LookupFlags.Sundries;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeCrawlingPipeComponent, AnchorStateChangedEvent>(OnAnchor);

        SubscribeLocalEvent<PipeCrawlingComponent, AtmosExposedGetAirEvent>(OnCrawlingExposed);
        SubscribeLocalEvent<PipeCrawlingComponent, InhaleLocationEvent>(OnCrawlingInhale);
        SubscribeLocalEvent<PipeCrawlingComponent, ExhaleLocationEvent>(OnCrawlingExhale);

        _nodeQuery = GetEntityQuery<NodeContainerComponent>();
    }

    private void OnAnchor(Entity<PipeCrawlingPipeComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (TerminatingOrDeleted(ent.Owner))
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

    private void OnCrawlingExposed(Entity<PipeCrawlingComponent> ent, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled)
            return;

        if (GetAir(ent) is { } air)
        {
            args.Gas = air;
            args.Handled = true;
        }
    }

    private void OnCrawlingInhale(Entity<PipeCrawlingComponent> ent, ref InhaleLocationEvent args)
    {
        if (GetAir(ent) is { } air)
            args.Gas = air;
    }

    private void OnCrawlingExhale(Entity<PipeCrawlingComponent> ent, ref ExhaleLocationEvent args)
    {
        if (GetAir(ent) is { } air)
            args.Gas = air;
    }

    protected override void PlaySound(Entity<PipeCrawlingPipeComponent> ent)
    {
        if (!_random.Prob(ent.Comp.MovingSoundProb))
            return;

        _audio.PlayPvs(ent.Comp.MovingSound, ent.Owner);
    }

    private GasMixture? GetAir(Entity<PipeCrawlingComponent> ent)
    {
        if (_internals.AreInternalsWorking(ent.Owner))
            return null;

        if (!_nodeQuery.TryComp(ent.Comp.CurrentPipe, out var nodeContainer))
            return null;

        var crawlerDirection = ent.Comp.Direction.ToPipeDirection().GetOpposite();

        foreach (var node in nodeContainer.Nodes.Values)
        {
            if (node is not PipeNode pipeNode)
                continue;

            if (pipeNode.CurrentPipeLayer != ent.Comp.CurrentLayer)
                continue;

            if (pipeNode.CurrentPipeDirection.HasFlag(crawlerDirection))
                return pipeNode.Air;
        }

        return null;
    }

    private void UpdatePipeConnections(Entity<PipeCrawlingPipeComponent> ent)
    {
        var pipeLayers = GetPipeLayers(ent.Owner);
        var pipeTile = Transform(ent.Owner).Coordinates.GetTileRef()?.GridIndices;

        ent.Comp.ConnectedPipes.Clear();

        if (pipeTile == null)
        {
            Dirty(ent);
            return;
        }

        if (HasComp<PipeCrawlingPipeBlockComponent>(ent.Owner))
        {
            Dirty(ent);
            return;
        }

        foreach (var neighbor in GetNeighbors(ent))
        {
            if (HasComp<PipeCrawlingPipeBlockComponent>(neighbor))
                continue;

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
                    var expectedNeighborPos = pipeTile.Value + directionVec;
                    if (neighborTile == expectedNeighborPos)
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

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return neighbors;

        var pipePos = xform.Coordinates;

        foreach (var direction in DirectionExtensions.AllDirections)
        {
            if (!IsCardinalDirection(direction))
                continue;

            var neighborPos = pipePos.Offset(direction.ToVec());
            var neighborTile = _map.CoordinatesToTile(gridUid, mapGrid, neighborPos);
            foreach (var neighbor in _lookup.GetLocalEntitiesIntersecting(gridUid, neighborTile, 0f, Flags, mapGrid))
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

                var oppositeDirection = direction.GetOpposite();
                if (!otherDirection.HasFlag(oppositeDirection))
                    continue;

                connectionDirections.Add(direction.ToDirection());
            }

            if (connectionDirections.Any())
                connections.Add(layer, connectionDirections);
        }

        return connections;
    }
}
