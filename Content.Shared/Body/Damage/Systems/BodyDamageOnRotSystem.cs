using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Damage.Components;

namespace Content.Shared.Body.Damage.Systems;

public sealed partial class BodyDamageOnRotSystem : EntitySystem
{
    [Dependency] private readonly BodyDamageableSystem _bodyDamageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyDamageOnRotComponent, RotUpdateEvent>(OnRotUpdate);
    }

    private void OnRotUpdate(Entity<BodyDamageOnRotComponent> ent, ref RotUpdateEvent args)
    {
        if (ent.Comp.MinRotStage > args.Stage || ent.Comp.MaxRotStage <= args.Stage)
        {
            ent.Comp.LastDamagePercentage = args.RotProgress;
            Dirty(ent);
            return;
        }

        var delta = args.RotProgress - ent.Comp.LastDamagePercentage;
        var damage = ent.Comp.FullyRottenDamage * delta;
        _bodyDamageable.ChangeDamage(ent.Owner, damage);

        ent.Comp.LastDamagePercentage = args.RotProgress;
        Dirty(ent);
    }
}
