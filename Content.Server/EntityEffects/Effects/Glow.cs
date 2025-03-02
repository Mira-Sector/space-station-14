using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     Makes a mob glow.
/// </summary>
public sealed partial class Glow : EntityEffect
{
    [DataField]
    public float Radius = 2f;

    [DataField]
    public Color Color = Color.Black;

    private static readonly List<Color> Colors = new()
    {
        Color.White,
        Color.Red,
        Color.Yellow,
        Color.Green,
        Color.Blue,
        Color.Purple,
        Color.Pink
    };

    public override void Effect(EntityEffectBaseArgs args)
    {
        var logMan = IoCManager.Resolve<ILogManager>();
        var log = logMan.RootSawmill;
        var lightSystem = args.EntityManager.System<SharedPointLightSystem>();
        var light = lightSystem.EnsureLight(args.TargetEntity);

        if (args is EntityEffectReagentArgs reagentArgs)
        {
            if (reagentArgs.Source == null || reagentArgs.Reagent == null)
                return;

            foreach (var quant in reagentArgs.Source.Contents.ToArray())
            {
                //log.Debug($"{quant.Reagent} {quant.Quantity} {reagentArgs.Reagent.ID} {reagentArgs.Quantity}");
                if (quant.Reagent.Prototype == reagentArgs.Reagent.ID && quant.Quantity - reagentArgs.Quantity <= 0)
                {
                    //log.Debug($"ded");
                    lightSystem.RemoveLightDeferred(args.TargetEntity);
                    return; //bad, will permenantly leave someone glowing if they vomit out the chemical just before it finishes
                }           //also means chemical has to be in their system the entire time. prefer to be more like a drug overlay.
            }
        }
        if (Color == Color.Black)
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            Color = random.Pick(Colors);
        }
        log.Debug($"isGlow");
        lightSystem.SetRadius(args.TargetEntity, Radius, light);
        lightSystem.SetColor(args.TargetEntity, Color, light);
        lightSystem.SetCastShadows(args.TargetEntity, false, light); // this is expensive, and botanists make lots of plants
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
