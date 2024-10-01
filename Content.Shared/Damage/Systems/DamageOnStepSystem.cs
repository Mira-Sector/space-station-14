using Content.Shared.Damage.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Damage.Systems;

public sealed class DamageOnStepSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamageOnStepComponent, EndCollideEvent>(OnCollide);
    }

    private void OnCollide(EntityUid uid, DamageOnStepComponent component, ref EndCollideEvent args)
    {
        if (!component.Enabled)
            return;
        _damageableSystem.TryChangeDamage(uid, component.Damage);
    }
}
