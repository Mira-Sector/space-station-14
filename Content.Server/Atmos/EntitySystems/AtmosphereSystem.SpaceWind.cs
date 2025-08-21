using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Audio;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    private static readonly ProtoId<SoundCollectionPrototype> SpaceWindSounds = "SpaceWind";

    private EntityQuery<MovedByPressureComponent> _movedByPressureQuery;

    private void InitializeSpaceWind()
    {
        _movedByPressureQuery = GetEntityQuery<MovedByPressureComponent>();
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
    }
}
