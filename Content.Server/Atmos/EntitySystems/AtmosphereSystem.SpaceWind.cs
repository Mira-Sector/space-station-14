using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
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

            UpdateSpaceWindMovableEntity((uid, moved, xform, physics));
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

    private void ProcessSpaceWindPressureFromSingleTile(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent, TileAtmosphere tile)
    {
        var tilePressure = tile.Air?.Pressure ?? 0f;
        tile.SpaceWind.PendingWind = Vector2.Zero;

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection)(1 << i);
            var neighbor = tile.AdjacentTiles[i];
            if (neighbor == null)
                continue;

            var neighborPressure = neighbor.Air?.Pressure ?? 0f;
            var dirVec = direction.CardinalToIntVec();

            tile.SpaceWind.PendingWind += dirVec * (tilePressure - neighborPressure);
        }

        tile.SpaceWind.PendingWind *= SpaceWindFlowRate;
    }

    private void ProcessSpaceWindNormalizationTile(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent, TileAtmosphere tile)
    {
        tile.SpaceWind.Wind = Vector2.Lerp(tile.SpaceWind.Wind, tile.SpaceWind.PendingWind, Atmospherics.SpaceWindNormalizationFactor);

        var pressure = tile.Air?.Pressure ?? 0f;
        var pressureSquared = pressure * pressure;
        if (tile.SpaceWind.Wind.LengthSquared() > pressureSquared)
            tile.SpaceWind.Wind = tile.SpaceWind.Wind.Normalized() * pressure;

        tile.SpaceWind.PendingWind = Vector2.Zero;
    }
}
