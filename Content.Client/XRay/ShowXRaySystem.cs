using Content.Client.Overlays;
using Content.Shared.Inventory.Events;
using Content.Shared.Physics;
using Content.Shared.XRay;
using Content.Shared.Whitelist;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using System.Linq;
using System.Numerics;

namespace Content.Client.XRay;

public sealed class ShowXRaySystem : EquipmentHudSystem<ShowXRayComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private XRayOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowXRayComponent> args)
    {
        base.UpdateInternal(args);

        if (!_overlayMan.HasOverlay<XRayOverlay>())
            _overlayMan.AddOverlay(_overlay);

        _overlay.Providers.Clear();

        _overlay.Providers.Clear();
        foreach (var component in args.Components)
            _overlay.Providers.Add((component.Owner, component));

        _overlay.Refresh();
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlayMan.RemoveOverlay(_overlay);
    }

    public IEnumerable<EntityUid> GetEntities(Entity<ShowXRayComponent> ent)
    {
        var entities = _lookup.GetEntitiesInRange(ent.Owner, ent.Comp.Range);

        var xrayTransform = Transform(ent.Owner);
        var xrayPos = _transform.GetWorldPosition(xrayTransform);
        var xrayMapId = xrayTransform.MapID;

        foreach (var entity in entities)
        {
            if (!_whitelist.CheckBoth(entity, ent.Comp.Blacklist, ent.Comp.Whitelist))
                continue;

            if (!HasComp<SpriteComponent>(entity))
                continue;

            var entityPos = _transform.GetWorldPosition(entity);

            // must be hidden
            if (!EntitiesBlocking(xrayPos, xrayMapId, entityPos))
                continue;

            yield return entity;
        }
    }

    public IEnumerable<TileRef> GetTiles(Entity<ShowXRayComponent> ent)
    {
        var xrayTransform = Transform(ent.Owner);
        var xrayPos = _transform.GetWorldPosition(xrayTransform);
        var xrayMapId = xrayTransform.MapID;

        var radius = new Circle(xrayPos, ent.Comp.Range);
        var grids = _lookup.GetEntitiesInRange<MapGridComponent>(xrayTransform.Coordinates, ent.Comp.Range);

        foreach (var (gridUid, gridComp) in grids)
        {
            var tiles = _map.GetTilesIntersecting(gridUid, gridComp, radius);

            var (gridPos, gridRot) = _transform.GetWorldPositionRotation(gridUid);

            foreach (var tile in tiles)
            {
                var tileLocalPos = _map.ToCenterCoordinates(tile, gridComp);
                var tilePos = gridPos + gridRot.RotateVec(tileLocalPos.Position);

                if (!EntitiesBlocking(xrayPos, xrayMapId, tilePos))
                    continue;

                yield return tile;
            }
        }
    }

    private bool EntitiesBlocking(Vector2 xrayPos, MapId xrayMapId, Vector2 targetPos)
    {
        var delta = targetPos - xrayPos;
        var distance = delta.Length();

        var ray = new CollisionRay(xrayPos, delta.Normalized(), (int)CollisionGroup.SingularityLayer);
        return _physics.IntersectRayWithPredicate(xrayMapId, ray, distance, e => !TryComp<OccluderComponent>(e, out var occluder) || !occluder.Enabled).Any();
    }
}
