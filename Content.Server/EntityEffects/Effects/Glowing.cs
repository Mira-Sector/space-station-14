using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Glowing;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     Makes a mob glow.
/// </summary>
///

public sealed partial class Glowing : EntityEffect
{
    public float GlowPower = 5f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var glowPower = GlowPower;

        if (args is EntityEffectReagentArgs reagentArgs)//if reagent, scale based on amount of reagent
        {
            glowPower *= reagentArgs.Scale.Float();
        }

        var glowSys = args.EntityManager.EntitySysManager.GetEntitySystem<GlowingSystem>();
        glowSys.DoGlow(args.TargetEntity, glowPower);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
