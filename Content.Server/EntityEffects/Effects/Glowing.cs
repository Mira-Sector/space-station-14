using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Glowing;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     Status Effect that makes you glow for some amount of time
/// </summary>
///

public sealed partial class Glowing : EntityEffect
{
    [DataField]
    public float GlowPower = 60f;

    [DataField]
    public float Radius = 4f;

    [DataField]
    public float Energy = 1f;

    [DataField]
    public Color Color = Color.Gold;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var glowPower = TimeSpan.FromSeconds(GlowPower);

        if (args is EntityEffectReagentArgs reagentArgs)//if reagent, scale based on amount of reagent
        {
            glowPower *= reagentArgs.Scale.Float();
        }

        var glowSys = args.EntityManager.EntitySysManager.GetEntitySystem<GlowingSystem>();
        glowSys.DoGlow(args.TargetEntity, glowPower);

        var entman = args.EntityManager;
        if (!entman.TryGetComponent(args.TargetEntity, out GlowingComponent? glowing)) //check to make sure glowing component was added, if it wasn't just stop here
            return;
        glowSys.UpdateGlowColor(args.TargetEntity, Color); //if it was, can update the glow to what we wanted
        glowSys.UpdateGlowRadius(args.TargetEntity, Radius);
        glowSys.UpdateGlowEnergy(args.TargetEntity, Energy);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    => Loc.GetString("reagent-effect-guidebook-glowing", ("chance", Probability));
}
