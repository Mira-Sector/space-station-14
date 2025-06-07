using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Random;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// replaces one chemical with an equal amount of another. If no replacement target, replace all chems. If no replacement chemical, randomly choose one from a WeightedRandomFillSolution list.
/// </summary>
public sealed partial class ReplaceChem : EntityEffect
{
    ///<summary>
    /// the chemical that will replace the target chemical, if left null will be chosen randomly based off of a WeightedRandomFillSolution that can be defined with "replaceList"
    ///</summary>
    [DataField]
    public string? ReplaceWith;

    ///<summary>
    /// the chemical that will be replaced. If left null all chemicals in the solution will be replaced instead.
    ///</summary>
    [DataField]
    public string? ReplaceTarget;

    ///<summary>
    /// WeightedRandomFillSolution List used if no ReplaceWith value set to pick a replacing chemical. Defaults to RandomPickBotanyReagent.
    ///</summary>
    [DataField]
    public string ReplaceList = "RandomPickBotanyReagent";

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.TryGetComponent<SolutionComponent>(args.TargetEntity, out var container))
            return; //if no solution component, go home.

        ReagentId replaceChem;
        if (ReplaceWith == null) //if no specific replacement chemical chosen, choose randomly.
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var randomChems = prototypeManager.Index<WeightedRandomFillSolutionPrototype>(ReplaceList).Fills;
            if (randomChems != null)
            {
                var pick = random.Pick<RandomFillSolution>(randomChems);
                replaceChem = new(random.Pick(pick.Reagents), null);
            }
            else
            {
                return;
            }
        }
        else //if specific replacement chemical chosen, use that.
        {
            replaceChem = new(ReplaceWith, null);
        }

        if (ReplaceTarget == null) //if no target, replace everything
        {
            var chemTotal = container.Solution.Volume;
            container.Solution.RemoveAllSolution();//purges all data. Could keep it by splitting and then adding but like, why?
            container.Solution.AddReagent(replaceChem, chemTotal);
        }
        else //if target chemical chosen, only replace that chemical
        {
            ReagentId targetChem = new(ReplaceTarget, null);
            var chemTargetAmount = container.Solution.GetReagentQuantity(targetChem);
            container.Solution.RemoveReagent(targetChem, chemTargetAmount);
            container.Solution.AddReagent(replaceChem, chemTargetAmount);
        }
    }
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
