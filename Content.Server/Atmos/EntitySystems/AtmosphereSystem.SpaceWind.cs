using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    private static readonly ProtoId<SoundCollectionPrototype> DefaultSpaceWindSounds = "SpaceWind";
    public SoundSpecifier? SpaceWindSound { get; private set; } = new SoundCollectionSpecifier(DefaultSpaceWindSounds, AudioParams.Default.WithVariation(0.125f));

    private TimeSpan _nextWindSound;

    private void UpdateSpaceWind(float frameTime)
    {
        UpdateSpaceWindMovable(frameTime);
        UpdateSpaceWindSound();
    }

    private void UpdateSpaceWindMovable(float frameTime)
    {
        var query = EntityQueryEnumerator<MovedByPressureComponent, TransformComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var moved, out var xform, out var physics))
        {
            if (TryGetMovableTileWind((uid, xform), out var wind))
            {
                // clamp
                if (wind.Value.IsLongerThan(SpaceWindMaxVelocity))
                    moved.CurrentWind = wind.Value.Normalized() * SpaceWindMaxVelocity;
                else
                    moved.CurrentWind = wind.Value;

                Dirty(uid, moved);
            }

            UpdateSpaceWindMovableEntity((uid, moved, xform, physics), frameTime);
        }
    }

    private bool TryGetMovableTileWind(Entity<TransformComponent> ent, [NotNullWhen(true)] out Vector2? wind)
    {
        wind = null;

        if (_simulationPaused)
            return false;

        if (ent.Comp.GridUid is not { } grid)
            return false;

        if (!_atmosQuery.TryComp(grid, out var mapAtmos))
            return false;

        if (!_mapGridQuery.TryComp(grid, out var mapGrid))
            return false;

        if (!TransformSystem.TryGetGridTilePosition(ent!, out var indices, mapGrid))
            return false;

        if (!mapAtmos.Tiles.TryGetValue(indices, out var tileAtmos))
            return false;

        wind = tileAtmos.SpaceWind.Wind;
        return true;
    }

    private void UpdateSpaceWindSound()
    {
        if (_simulationPaused)
            return;

        if (_nextWindSound > GameTiming.CurTime)
            return;

        var query = EntityQueryEnumerator<GridAtmosphereComponent, MapGridComponent>();
        while (query.MoveNext(out var grid, out var gridAtmos, out var mapGrid))
        {
            HashSet<Vector2i> soundBlockedTiles = new(gridAtmos.SpaceWindSoundTilesCount * SpaceWindSoundRange * Atmospherics.Directions);

            foreach (var tile in gridAtmos.SpaceWindSoundTiles)
            {
                if (!soundBlockedTiles.Add(tile.GridIndices))
                    continue;

                // sound emits from the strongest tile in the cluster
                var strongestTile = tile;
                Queue<(TileAtmosphere, int)> queue = new();
                queue.Enqueue((tile, 0));

                while (queue.TryDequeue(out var queueOut))
                {
                    var (current, depth) = queueOut;

                    for (var i = 0; i < Atmospherics.Directions; i++)
                    {
                        var direction = (AtmosDirection)(1 << i);
                        var adjIndices = current.GridIndices + direction.CardinalToIntVec();

                        if (soundBlockedTiles.Contains(adjIndices))
                            continue;

                        if (!gridAtmos.Tiles.TryGetValue(adjIndices, out var adjTile))
                            continue;

                        if (adjTile.SpaceWind.Wind.Length() < SpaceWindMinSoundMagnitude)
                            continue;

                        if (depth++ < SpaceWindSoundRange)
                            queue.Enqueue((adjTile, depth));

                        soundBlockedTiles.Add(adjIndices);

                        if (adjTile.SpaceWind.Wind.Length() > strongestTile.SpaceWind.Wind.Length())
                            strongestTile = adjTile;
                    }
                }

                // play sound at strongest tile in the cluster
                var coords = _map.ToCenterCoordinates(grid, strongestTile.GridIndices, mapGrid);
                _audio.PlayPvs(SpaceWindSound, coords);
            }

            gridAtmos.SpaceWindSoundTiles.Clear();
        }

        _nextWindSound += SpaceWindSoundCooldown;
    }

    private void ProcessSpaceWindPressureFromSingleTile(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent, TileAtmosphere startTile)
    {
        Queue<(TileAtmosphere, int)> propagationQueue = [];
        HashSet<TileAtmosphere> visited = [];

        propagationQueue.Enqueue((startTile, 0));
        visited.Add(startTile);

        while (propagationQueue.TryDequeue(out var current))
        {
            var (tile, layer) = current;
            if (layer > Atmospherics.SpaceWindLayerPropergation)
                continue;

            ProcessSpaceWindPressureTile(ent, tile, propagationQueue, visited, layer);
        }

        foreach (var tile in visited)
        {
            tile.SpaceWind.Wind += tile.SpaceWind.PendingWind;
            tile.SpaceWind.PendingWind = Vector2.Zero;

            var pressure = tile.Air?.Pressure ?? 0f;
            CapWindByPressure(tile, pressure);

            if (tile.SpaceWind.Wind.Length() > SpaceWindMinSoundMagnitude)
                ent.Comp1.SpaceWindSoundTiles.Add(tile);
        }
    }

    private void ProcessSpaceWindPressureTile(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent, TileAtmosphere tile, Queue<(TileAtmosphere, int)> queue, HashSet<TileAtmosphere> visited, int currentLayer)
    {
        var selfPressure = tile.Air?.Pressure ?? 0f;

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection)(1 << i);
            var adjacent = tile.AdjacentTiles[i];
            if (adjacent == null || visited.Contains(adjacent))
                continue;

            var dirVec = direction.CardinalToIntVec();
            ApplyWindContribution(tile, adjacent, dirVec, selfPressure, ent, queue, visited, currentLayer);
        }

        var sqrt2 = MathF.Sqrt(2);
        var sqrt2Divisor = 1 / sqrt2;
        HashSet<AtmosDirection> visitedDirections = new(Atmospherics.Directions * 2);

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var directionI = (AtmosDirection)(1 << i);
            var adjacentI = tile.AdjacentTiles[i];
            if (adjacentI == null)
                continue;

            for (var j = 0; j < Atmospherics.Directions; j++)
            {
                if (i == j || i.ToOppositeIndex() == j)
                    continue;

                var directionJ = (AtmosDirection)(1 << j);
                var adjacentJ = adjacentI.AdjacentTiles[j];
                if (adjacentJ == null)
                    continue;

                var direction = directionI | directionJ;
                var dirVec = direction.DirectionToIntVec() * sqrt2Divisor;

                if (visitedDirections.Add(direction))
                    ApplyWindContribution(tile, adjacentJ, dirVec, selfPressure, ent, queue, visited, currentLayer);
            }
        }
    }

    private void ApplyWindContribution(
        TileAtmosphere from,
        TileAtmosphere to,
        Vector2 direction,
        float fromPressure,
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
        Queue<(TileAtmosphere, int)> queue,
        HashSet<TileAtmosphere> visited,
        int currentLayer)
    {
        var toPressure = to.Air?.Pressure ?? 0f;

        var diff = fromPressure - toPressure;
        if (diff <= 0f)
            return;

        if (to.Air == null)
        {
            // this is space bitch
            // suck all that shit out right now
            FloodVacuumInstant(ent, from, to, diff, direction, visited);
        }
        else
        {
            var flowFraction = MathF.Min(1f, diff / SpaceWindBreachThreshold);
            var falloff = MathF.Max(0.1f, toPressure / fromPressure);
            var contribution = direction * (diff * SpaceWindFlowRate * flowFraction) * falloff;

            from.SpaceWind.PendingWind += contribution;
            to.SpaceWind.PendingWind -= contribution;


            if (visited.Add(to))
                queue.Enqueue((to, currentLayer + 1));
        }
    }

    private void FloodVacuumInstant(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent, TileAtmosphere from, TileAtmosphere start, float initialDiff, Vector2 dirVec, HashSet<TileAtmosphere> visited)
    {
        Queue<TileAtmosphere> queue = [];
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.TryDequeue(out var tile))
        {
            // flow contribution into this vacuum
            var contribution = dirVec * (initialDiff * SpaceWindFlowRate * SpaceWindVacuumMultiplier);

            from.SpaceWind.PendingWind += contribution;
            tile.SpaceWind.PendingWind -= contribution;

            ent.Comp1.CurrentRunTiles.Enqueue(tile);

            // continue flood into further vacuums
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var adj = tile.AdjacentTiles[i];
                if (adj == null || adj.Air != null)
                    continue;

                if (visited.Add(adj))
                    queue.Enqueue(adj);
            }
        }
    }

    private void ProcessSpaceWindNormalizationTile(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent, TileAtmosphere tile)
    {
        var normal = tile.SpaceWind.Wind.Normalized();
        var length = tile.SpaceWind.Wind.Length();

        if (length == 0)
            length = 1;
        else
            length *= Atmospherics.SpaceWindNormalizationFactor;

        var newVec = normal * length;
        tile.SpaceWind.Wind = newVec;
    }

    private static void CapWindByPressure(TileAtmosphere tile, float pressure)
    {
        var mag = tile.SpaceWind.Wind.Length();

        if (mag > pressure)
            tile.SpaceWind.Wind = tile.SpaceWind.Wind.Normalized() * pressure;
    }
}
