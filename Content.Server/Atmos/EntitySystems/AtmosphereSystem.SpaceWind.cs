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

        if (!TransformSystem.TryGetGridTilePosition(ent!, out var indices))
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

    private void ProcessSpaceWindTile(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent, TileAtmosphere tile)
    {
        ent.Comp1.SpaceWindTiles.Remove(tile);

        tile.SpaceWind.Wind = Vector2.Zero;

        var selfMix = tile.Air;
        var selfPressure = selfMix?.Pressure ?? 0f;

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection)(1 << i);
            var adjacent = tile.AdjacentTiles[i];
            if (adjacent == null)
                continue;

            var adjMix = adjacent.Air;
            var adjPressure = adjMix?.Pressure ?? 0f;

            var diff = selfPressure - adjPressure;
            if (diff <= 0f)
                continue;

            var flowFraction = MathF.Min(1f, diff / SpaceWindBreachThreshold);
            var dirVec = direction.CardinalToIntVec();

            var multiplier = (adjMix == null) ? SpaceWindVacuumMultiplier : 1f;
            var contribution = dirVec * (diff * flowFraction * SpaceWindFlowRate * multiplier);

            tile.SpaceWind.Wind += contribution;
            adjacent.SpaceWind.Wind -= contribution;
        }

        // cap by local pressure so wind doesnt go crazy
        if (tile.SpaceWind.Wind != Vector2.Zero)
        {
            var maxMag = selfPressure;
            var mag = tile.SpaceWind.Wind.Length();

            if (mag > maxMag)
                tile.SpaceWind.Wind = tile.SpaceWind.Wind.Normalized() * maxMag;
        }

        if (tile.SpaceWind.Wind.Length() > SpaceWindMinSoundMagnitude)
            ent.Comp1.SpaceWindSoundTiles.Add(tile);
    }
}
