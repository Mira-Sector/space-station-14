using Content.Shared.Body.Damage.Components;
using Content.Shared.Body.Damage.Events;
using Content.Shared.Damage;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Damage.Systems;

public sealed partial class DamageOnBodyDamageSystem : BaseOnBodyDamageSystem<DamageOnBodyDamageComponent>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnBodyDamageComponent, BodyDamageChangedEvent>(OnDamageChanged, after: [typeof(BodyDamageThresholdsSystem)]);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DamageOnBodyDamageComponent, BodyDamageableComponent>();
        while (query.MoveNext(out var uid, out var component, out var damageable))
        {
            if (component.DamageDelay is not { } damageDelay)
                continue;

            if (component.NextDamage > _timing.CurTime)
                continue;

            component.NextDamage += damageDelay;

            if (!CanDoEffect((uid, component, null, damageable)))
            {
                Dirty(uid, component);
                continue;
            }

            _damageable.TryChangeDamage(uid, component.Damage);
            Dirty(uid, component);
        }
    }

    private void OnDamageChanged(Entity<DamageOnBodyDamageComponent> ent, ref BodyDamageChangedEvent args)
    {
        if (ent.Comp.DamageDelay != null)
            return;

        if (!CanDoEffect(ent))
            return;

        var oldDamage = args.OldDamage - ent.Comp.MinDamage;
        var delta = oldDamage - args.NewDamage;
        if (!CheckMode(ent, delta > 0))
            return;

        var damage = ent.Comp.Damage * delta;
        _damageable.TryChangeDamage(ent.Owner, damage);
    }

    protected override bool CanDoEffect(Entity<DamageOnBodyDamageComponent, BodyDamageThresholdsComponent?, BodyDamageableComponent?> ent)
    {
        if (!base.CanDoEffect(ent))
            return false;

        return true;
    }

    private static bool CheckMode(Entity<DamageOnBodyDamageComponent> ent, bool positive)
    {
        if (positive)
            return ent.Comp.Mode.HasFlag(DamageOnBodyDamageModes.Damage);
        else
            return ent.Comp.Mode.HasFlag(DamageOnBodyDamageModes.Healing);
    }
}
