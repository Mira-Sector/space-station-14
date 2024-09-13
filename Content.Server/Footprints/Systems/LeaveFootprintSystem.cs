using Content.Server.Decals;
using Content.Server.Footprints.Components;
using Content.Shared.Decals;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Server.Footprint.Systems;


public sealed class LeaveFootprintystem : EntitySystem
{

    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    const float MaxAlpha = 0.5f;
    const float MinAlpha = 0.2f;

    const float DeltaAlpha = MaxAlpha - MinAlpha;

    public override void Update(float frametime)
    {
        var query = EntityQueryEnumerator<CanLeaveFootprintsComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var currentFootprintComp, out var transform))
        {
            if (!EntityManager.TryGetComponent<LeavesFootprintsComponent>(uid, out var playerFootprintComp))
            {
                RemComp<CanLeaveFootprintsComponent>(uid);
                continue;
            }

            var newPos = _transform.GetMapCoordinates(uid);
            var oldPos = currentFootprintComp.LastFootstep;

            if (newPos == MapCoordinates.Nullspace)
                continue;

            var worldPos = _transform.GetWorldPosition(uid);

            var angle = transform.LocalRotation;

            if (newPos.MapId != oldPos.MapId)
            {
                DoFootprint(uid, currentFootprintComp, playerFootprintComp, newPos, angle);
                return;
            }

            var delta = Vector2.Distance(newPos.Position, oldPos.Position);

            if (delta < playerFootprintComp.Distance)
                continue;

            DoFootprint(uid, currentFootprintComp, playerFootprintComp, newPos, angle);
        }
    }

    private void DoFootprint(EntityUid uid, CanLeaveFootprintsComponent currentFootprintComp, LeavesFootprintsComponent playerFootprintComp, MapCoordinates pos, Angle angle)
    {
        var decal = playerFootprintComp.FootprintDecal;

        if (currentFootprintComp.UseAlternative != null)
        {
            if (currentFootprintComp.UseAlternative.Value)
                decal = playerFootprintComp.FootprintDecalAlternative;

            currentFootprintComp.UseAlternative ^= true;
        }

        if (!_prototypeManager.TryIndex<DecalPrototype>(decal, out var footprintDecal))
        {
            RemComp<CanLeaveFootprintsComponent>(uid);
            return;
        }

        if (!_mapManager.TryFindGridAt(pos, out var gridUid, out var grid))
            return;

        var color = currentFootprintComp.Color;

        float onePiece = DeltaAlpha / (playerFootprintComp.MaxFootsteps); //IS REAL!!
        color = color.WithAlpha(MinAlpha + (onePiece * currentFootprintComp.FootstepsLeft));

        var coords = new EntityCoordinates(gridUid, _map.WorldToLocal(gridUid, grid, pos.Position));

        _decals.TryAddDecal(footprintDecal.ID, coords, out _, color, angle, cleanable: true);

        currentFootprintComp.FootstepsLeft -= 1;

        if (currentFootprintComp.FootstepsLeft <= 0)
        {
            RemComp<CanLeaveFootprintsComponent>(uid);
            return;
        }

        currentFootprintComp.LastFootstep = pos;
        currentFootprintComp.Color = color;
    }
}
