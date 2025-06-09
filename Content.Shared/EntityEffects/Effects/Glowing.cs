using Robust.Shared.Prototypes;
using Content.Shared.Glowing;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
///     Status Effect that makes you glow for some amount of time
/// </summary>
public sealed partial class Glowing : EntityEffect
{
    [DataField]
    public TimeSpan GlowTime = TimeSpan.FromSeconds(60);

    [DataField]
    public float Radius = 4f;

    [DataField]
    public float Energy = 1f;

    [DataField]
    public Color Color = Color.Gold;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var time = GlowTime;

        //if reagent, scale based on amount of reagent
        if (args is EntityEffectReagentArgs reagentArgs)
            time *= reagentArgs.Scale.Float();

        var glowSys = args.EntityManager.EntitySysManager.GetEntitySystem<GlowingSystem>();
        glowSys.DoGlow(args.TargetEntity, time);

        var entman = args.EntityManager;

        //check to make sure glowing component was added, if it wasn't just stop here
        if (!entman.HasComponent<GlowingComponent>(args.TargetEntity))
            return;

        glowSys.UpdateGlowColor(args.TargetEntity, Color); //if it was, can update the glow to what we wanted
        glowSys.UpdateGlowRadius(args.TargetEntity, Radius);
        glowSys.UpdateGlowEnergy(args.TargetEntity, Energy);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    => Loc.GetString("reagent-effect-guidebook-glowing", ("chance", Probability));
}
