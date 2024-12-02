using Content.Shared.Projectiles;

namespace Content.Server.Projectiles;

public sealed class ProjectilePenetrationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectilePenetrationComponent, ProjectileHitEvent>(OnHit);
    }

    private void OnHit(EntityUid uid, ProjectilePenetrationComponent component, ref ProjectileHitEvent args)
    {
        if (!TryComp<ProjectileComponent>(uid, out var projectileComp))
            return;

        projectileComp.DamagedEntity = false;
        Dirty(uid, projectileComp);

        if (component.CollidedEntities.Contains(args.Target))
            return;

        component.CollidedEntities.Add(args.Target);

        if (component.CollidedEntities.Count <= component.Ammount)
            return;

        projectileComp.DeleteOnCollide = true;
        Dirty(uid, projectileComp);
    }
}
