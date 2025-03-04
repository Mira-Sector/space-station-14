using Content.Shared.Body.Events;
using Content.Shared.Damage.Components;

namespace Content.Shared.Damage.Systems;

public sealed class DamageOnSpawnSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnSpawnComponent, ComponentInit>((u, c, a) => OnInit((u, c)));
        SubscribeLocalEvent<DamageOnSpawnComponent, BodyInitEvent>((u, c, a) => OnInit((u, c)));
    }

    private void OnInit(Entity<DamageOnSpawnComponent> ent)
    {
        _damageable.TryChangeDamage(ent, ent.Comp.Damage, splitLimbDamage: ent.Comp.SplitLimbDamage);
    }
}
