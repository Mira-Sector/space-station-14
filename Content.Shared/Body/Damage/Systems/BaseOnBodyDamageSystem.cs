using Content.Shared.Body.Damage.Components;

namespace Content.Shared.Body.Damage.Systems;

public abstract partial class BaseOnBodyDamageSystem<T> : EntitySystem where T : BaseOnBodyDamageComponent
{
    [MustCallBase]
    protected virtual bool CanDoEffect(Entity<T, BodyDamageThresholdsComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp2, false))
            return true;

        return ent.Comp1.RequiredStates.Contains(ent.Comp2.CurrentState);
    }
}
