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

        var ev = new BodyDamageChangedEvent(ent.Comp.Damage, targetDamage);
        RaiseLocalEvent(ent.Owner, ref ev);

        ent.Comp.Damage = ev.NewDamage;
        Dirty(ent);
    }

    [PublicAPI]
    public void SetDamage(Entity<BodyDamageableComponent?> ent, FixedPoint2 damage, bool force = false)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (!CanDamage(ent!, damage, force))
            return;

        var ev = new BodyDamageChangedEvent(ent.Comp.Damage, damage);
        RaiseLocalEvent(ent.Owner, ref ev);

        ent.Comp.Damage = ev.NewDamage;
        Dirty(ent);
    }

    private bool CanDamage(Entity<BodyDamageableComponent> ent, FixedPoint2 damage, bool force)
    {
        if (damage < FixedPoint2.Zero)
            return false;

        if (force)
            return true;

        var ev = new BodyDamageCanDamageEvent(ent.Comp.Damage, damage);
        RaiseLocalEvent(ent.Owner, ev);

        return !ev.Cancelled;
    }
}
