using Content.Shared.Body.Damage.Components;
using Content.Shared.Body.Damage.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Damage.Systems;

public sealed partial class DamageOnBodyDamageSystem : BaseOnBodyDamageSystem<DamageOnBodyDamageComponent>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly BodyDamageThresholdsSystem _thresholds = default!;
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

            var damage = GetScaledDamage((uid, component));
            DealDamage(uid, damage);
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

        var damage = GetScaledDamage(ent) * delta;
        DealDamage(ent.Owner, damage);
    }

    private static bool CheckMode(Entity<DamageOnBodyDamageComponent> ent, bool positive)
    {
        if (positive)
            return ent.Comp.Mode.HasFlag(DamageOnBodyDamageModes.Damage);
        else
            return ent.Comp.Mode.HasFlag(DamageOnBodyDamageModes.Healing);
    }

    private DamageSpecifier GetScaledDamage(Entity<DamageOnBodyDamageComponent, BodyDamageableComponent?, BodyDamageThresholdsComponent?> ent)
    {
        if (ent.Comp1.ScaleToState is not { } targetState)
            return ent.Comp1.Damage;

        if (!_thresholds.TryGetThreshold((ent.Owner, ent.Comp3), targetState, out var threshold))
            return ent.Comp1.Damage;

        var toState = _thresholds.RelativeToState((ent.Owner, ent.Comp3, ent.Comp2), targetState);
        if (toState == FixedPoint2.Zero)
            return ent.Comp1.Damage;

        var ratio = toState / threshold;
        return ent.Comp1.Damage * ratio;
    }

    private void DealDamage(EntityUid uid, DamageSpecifier damage)
    {
        if (TryComp<OrganComponent>(uid, out var organ))
        {
            if (organ.Body is { } body)
                _damageable.TryChangeDamageBody(body, damage);
            else if (organ.BodyPart is { } limb)
                _damageable.TryChangeDamage(limb, damage);

            return;
        }

        _damageable.TryChangeDamage(uid, damage);
    }
}
