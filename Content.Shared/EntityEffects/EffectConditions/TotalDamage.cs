using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.EffectConditions;

public sealed partial class TotalDamage : EntityEffectCondition
{
    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        FixedPoint2? total = null;

        if (args.EntityManager.TryGetComponent(args.TargetEntity, out DamageableComponent? damage))
            total = damage.TotalDamage;
        else if (args.EntityManager.System<SharedBodySystem>().GetBodyDamage(args.TargetEntity) is {} bodyDamage)
            total = bodyDamage.GetTotal();

        if (total > Min && total < Max)
            return true;

        return false;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-total-damage",
            ("max", Max == FixedPoint2.MaxValue ? (float) int.MaxValue : Max.Float()),
            ("min", Min.Float()));
    }
}
