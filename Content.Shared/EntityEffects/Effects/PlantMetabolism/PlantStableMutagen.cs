using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantStableMutagen : EventEntityEffect<PlantStableMutagen>
{
    ///<summary>
    /// maximum of reagent allowed before falloff in added amount, don't go below 1
    ///</summary>
    public float MaxChem = 5;

    ///<summary>
    /// Min chem to add per cycle
    ///</summary>
    public float Min = 0.5f;

    ///<summary>
    /// Max chem to add per cycle
    ///</summary>
    public float Max = 1f;

    ///<summary>
    /// Falloff when over maximum reagent amount, applied as an exponent (aka, 2 = fall off with the square of the existing amount)
    ///</summary>
    public float Falloff = 1.5f;

    ///<summary>
    /// Chance for new reagent to be added to a plant, reagents that already exist are guarenteed to be increased.
    /// Can't use Base Probability that comes with entity effects due to second part and due to how the probability changes dynamically
    /// </summary
    public float BaseReagentAddChance = 0.3f;

    ///<summary>
    /// Maximum number of non-inherent reagents (aka added via crossbreeding, mutation, or this effect) in produce before effect starts to lose efficacy in adding new ones
    ///</summary>
    public float MaxReagentCount = 2;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-plant-stable-mutagen", ("chance", BaseReagentAddChance), ("percent", (BaseReagentAddChance * 100).ToString()), ("reagentCount", MaxReagentCount));
}
