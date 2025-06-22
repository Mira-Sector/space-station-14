using Content.Shared.Body.Damage.Components;
using Content.Shared.Body.Damage.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;
using JetBrains.Annotations;

namespace Content.Shared.Body.Damage.Systems;

public sealed partial class BodyDamageableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyDamageableComponent, RejuvenateEvent>(OnRejuvenated);
    }

    private void OnRejuvenated(Entity<BodyDamageableComponent> ent, ref RejuvenateEvent args)
    {
        SetDamage(ent.AsNullable(), FixedPoint2.Zero);
    }

    [PublicAPI]
    public void ChangeDamage(Entity<BodyDamageableComponent?> ent, FixedPoint2 damage, bool force = false)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var targetDamage = ent.Comp.Damage + damage;
        if (!CanDamage(ent!, targetDamage, force))
            return;

        var oldDamage = ent.Comp.Damage;
        ent.Comp.Damage = targetDamage;
        Dirty(ent);
        var ev = new BodyDamageChangedEvent(oldDamage, ent.Comp.Damage);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    [PublicAPI]
    public void SetDamage(Entity<BodyDamageableComponent?> ent, FixedPoint2 damage, bool force = false)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (!CanDamage(ent!, damage, force))
            return;

        var oldDamage = ent.Comp.Damage;
        ent.Comp.Damage = damage;
        Dirty(ent);
        var ev = new BodyDamageChangedEvent(oldDamage, ent.Comp.Damage);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    private bool CanDamage(Entity<BodyDamageableComponent> ent, FixedPoint2 damage, bool force)
    {
        if (force)
            return true;

        var ev = new BodyDamageCanDamageEvent(ent.Comp.Damage, damage);
        RaiseLocalEvent(ent.Owner, ev);

        return !ev.Cancelled;
    }
}
