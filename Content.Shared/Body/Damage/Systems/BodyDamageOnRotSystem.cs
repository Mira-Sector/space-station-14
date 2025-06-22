using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Damage.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared.Body.Damage.Systems;

public sealed partial class BodyDamageOnRotSystem : EntitySystem
{
    [Dependency] private readonly BodyDamageableSystem _bodyDamageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyDamageOnRotComponent, RotUpdateEvent>(OnRotUpdate);
        SubscribeLocalEvent<BodyDamageOnRotComponent, StartedRottingEvent>(OnStartedRotting);
    }

    private void OnRotUpdate(Entity<BodyDamageOnRotComponent> ent, ref RotUpdateEvent args)
    {
        if (!RotStageWithinBounds(ent, args.Stage))
        {
            ent.Comp.LastDamagePercentage = args.RotProgress;
            Dirty(ent);
            return;
        }

        UpdateRotDamage(ent, args.RotProgress);
    }

    private void OnStartedRotting(Entity<BodyDamageOnRotComponent> ent, ref StartedRottingEvent args)
    {
        var perishable = Comp<PerishableComponent>(ent.Owner);
        if (!RotStageWithinBounds(ent, perishable.Stage))
            return;

        UpdateRotDamage(ent, 1f);
    }

    private void UpdateRotDamage(Entity<BodyDamageOnRotComponent> ent, float percentage)
    {
        var delta = percentage - ent.Comp.LastDamagePercentage;
        var damage = (float)ent.Comp.FullyRottenDamage * delta;
        // this is done due to floating point precision
        // otherwise you end up in situations where max damage never truly occurs and falling a tad short
        _bodyDamageable.ChangeDamage(ent.Owner, FixedPoint2.NewCeiling(damage));

        ent.Comp.LastDamagePercentage = percentage;
        Dirty(ent);
    }

    private static bool RotStageWithinBounds(Entity<BodyDamageOnRotComponent> ent, int stage)
    {
        return ent.Comp.MinRotStage <= stage || ent.Comp.MaxRotStage >= stage;
    }
}
