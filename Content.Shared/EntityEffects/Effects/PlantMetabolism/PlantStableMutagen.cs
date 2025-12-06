using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantStableMutagen : EventEntityEffect<PlantStableMutagen>
{
    public float MaxChem = 5; //maximum of reagent allowed before falloff in added amount, don't go below 1
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-plant-stable-mutagen", ("chance", Probability));
}
