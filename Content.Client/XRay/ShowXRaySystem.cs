using Content.Client.Overlays;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Physics;
using Content.Shared.XRay;
using Content.Shared.Whitelist;
using Robust.Client.Graphics;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using System.Linq;
using Robust.Client.GameObjects;

namespace Content.Client.XRay;

public sealed class ShowXRaySystem : EquipmentHudSystem<ShowXRayComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private XRayOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShowXRayComponent, ComponentInit>(OnInit);

        _overlay = new();
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ShowXRayComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.NextRefresh > _timing.CurTime)
                continue;

            if (TryComp<ClothingComponent>(uid, out var clothing) && clothing.InSlot == null)
            {
                component.NextRefresh += component.RefreshTime;
                continue;
            }

            _overlay.Entities.Remove((uid, component));
            UpdateEntities((uid, component));
        }
    }

    private void OnInit(Entity<ShowXRayComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextRefresh = _timing.CurTime + ent.Comp.RefreshTime;
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowXRayComponent> args)
    {
        base.UpdateInternal(args);

        if (!_overlayMan.HasOverlay<XRayOverlay>())
            _overlayMan.AddOverlay(_overlay);

        _overlay.Entities.Clear();

        foreach (var component in args.Components)
        {
            UpdateEntities((component.Owner, component));
        }
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void UpdateEntities(Entity<ShowXRayComponent> ent)
    {
        _overlay.Entities.Add(ent, new HashSet<EntityUid>());
        var entities = _lookup.GetEntitiesInRange(ent.Owner, ent.Comp.Range);

        var xrayTransform = Transform(ent.Owner);
        var xrayPos = _transform.GetWorldPosition(xrayTransform);
        var xrayMapId = xrayTransform.MapID;

        foreach (var entity in entities)
        {
            if (!_whitelist.CheckBoth(entity, ent.Comp.Blacklist, ent.Comp.Whitelist))
                continue;

            if (!TryComp<SpriteComponent>(entity, out var sprite))
                continue;

            var entityPos = _transform.GetWorldPosition(entity);
            var delta = entityPos - xrayPos;
            var distance = delta.Length();

            var ray = new CollisionRay(xrayPos, delta.Normalized(), (int)CollisionGroup.SingularityLayer);
            var rayCastResults = _physics.IntersectRayWithPredicate(xrayMapId, ray, distance, e => !HasComp<OccluderComponent>(e) || e == entity);

            // must be hidden
            if (!rayCastResults.Any())
                continue;

            _overlay.Entities[ent].Add(entity);
        }

        ent.Comp.NextRefresh += ent.Comp.RefreshTime;
    }
}
