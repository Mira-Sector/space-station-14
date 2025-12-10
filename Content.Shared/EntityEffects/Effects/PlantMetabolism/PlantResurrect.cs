using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantResurrect : EventEntityEffect<PlantResurrect>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-plant-resurrect", ("chance", Probability));
}
