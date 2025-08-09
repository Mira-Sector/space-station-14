using Content.Shared.Body.Damage.Components;
using Content.Shared.Body.Damage.Events;

namespace Content.Shared.Body.Damage.Systems;

public abstract partial class BaseToggleOnBodyDamageSystem<T> : BaseOnBodyDamageSystem<T> where T : BaseToggleOnBodyDamageComponent
{
    [MustCallBase]
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, ComponentInit>(OnInit);
        SubscribeLocalEvent<T, BodyDamageChangedEvent>(OnDamage, after: [typeof(BodyDamageThresholdsSystem)]);
    }

    [MustCallBase]
    protected void OnInit(Entity<T> ent, ref ComponentInit args)
    {
        ent.Comp.Enabled = CanDoEffect(ent);
    }

    [MustCallBase]
    protected void OnDamage(Entity<T> ent, ref BodyDamageChangedEvent args)
    {
        ent.Comp.Enabled = CanDoEffect(ent);
    }
}
