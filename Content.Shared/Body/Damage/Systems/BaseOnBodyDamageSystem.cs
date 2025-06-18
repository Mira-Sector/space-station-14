using Content.Shared.Body.Damage.Components;

namespace Content.Shared.Body.Damage.Systems;

public abstract partial class BaseOnBodyDamageSystem<T> : EntitySystem where T : BaseOnBodyDamageComponent
{
    [MustCallBase]
    protected virtual bool CanDoEffect(Entity<T, BodyDamageThresholdsComponent?, BodyDamageableComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp2, ref ent.Comp3, false))
            return true;

        if (!ent.Comp1.RequiredStates.Contains(ent.Comp2.CurrentState))
            return false;

        return ent.Comp3.Damage > ent.Comp1.MinDamage && ent.Comp3.Damage < ent.Comp1.MaxDamage;
    }
}
