using Content.Shared.Body.Damage.Components;
using Content.Shared.Body.Damage.Events;

namespace Content.Shared.Body.Damage.Systems;

public abstract partial class BaseToggleOnBodyDamageSystem<T> : BaseOnBodyDamageSystem<T> where T : BaseToggleOnBodyDamageComponent
{
    [MustCallBase]
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, BodyDamageChangedEvent>(OnDamage, after: [typeof(BodyDamageThresholdsSystem)]);
    }

    [MustCallBase]
    protected void OnDamage(Entity<T> ent, ref BodyDamageChangedEvent args)
    {
        ent.Comp.Enabled = CanDoEffect(ent);
    }
}
