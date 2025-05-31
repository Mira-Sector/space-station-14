using Content.Server.Supermatter.Components;

namespace Content.Server.Supermatter.Delaminations;

[DataDefinition]
public sealed partial class EnergyRequirement : DelaminationRequirement
{
    [DataField]
    public float Min = float.MinValue;

    [DataField]
    public float Max = float.MaxValue;

    public override bool RequirementMet(EntityUid supermatter, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<SupermatterEnergyComponent>(supermatter, out var energyComp))
            return false;

        return energyComp.CurrentEnergy > Min && energyComp.CurrentEnergy < Max;
    }
}
