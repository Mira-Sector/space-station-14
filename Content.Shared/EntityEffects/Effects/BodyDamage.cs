using Content.Shared.Body.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class BodyDamage : EntityEffect
{
    [DataField(required: true)]
    public FixedPoint2 Damage;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-body-damage",
                ("amount", Damage.Float()),
                ("deltasign", FixedPoint2.Sign(Damage)));

    public override void Effect(EntityEffectBaseArgs args)
    {
        FixedPoint2 damage;
        EntityUid toDamage;

        if (args is EntityEffectReagentArgs reagentArgs)
        {
            damage = Damage * reagentArgs.Quantity;
            toDamage = reagentArgs.OrganEntity ?? args.TargetEntity;
        }
        else
        {
            damage = Damage;
            toDamage = args.TargetEntity;
        }

        var bodyDamageSys = args.EntityManager.EntitySysManager.GetEntitySystem<BodyDamageableSystem>();
        bodyDamageSys.ChangeDamage(toDamage, damage);
    }
}
