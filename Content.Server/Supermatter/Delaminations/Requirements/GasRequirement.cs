using Content.Server.Supermatter.Components;
using Content.Shared.Atmos;

namespace Content.Server.Supermatter.Delaminations;

[DataDefinition]
public sealed partial class GasRequirement : DelaminationRequirement
{
    [DataField]
    public Dictionary<Gas, float> RequiredGas = new();

    public override bool RequirementMet(EntityUid supermatter, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<SupermatterGasAbsorberComponent>(supermatter, out var gasAbsorberComp))
            return false;

        foreach (var (gas, requirement) in RequiredGas)
        {
            if (requirement <= 0f)
                continue;

            if (!gasAbsorberComp.AbsorbedMoles.TryGetValue(gas, out var moles))
                return false;

            var percentage = moles / gasAbsorberComp.TotalMoles;

            if (percentage > requirement)
                return false;
        }

        return true;
    }
}
