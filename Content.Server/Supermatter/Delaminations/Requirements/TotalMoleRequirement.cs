using Content.Server.Supermatter.Components;

namespace Content.Server.Supermatter.Delaminations;

[DataDefinition]
public sealed partial class TotalMoleRequirement : DelaminationRequirement
{
    [DataField]
    public float Min = float.MinValue;

    [DataField]
    public float Max = float.MaxValue;

    public override bool RequirementMet(EntityUid supermatter, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<SupermatterGasAbsorberComponent>(supermatter, out var gasAbsorberComp))
            return false;

        float totalMoles = 0f;

        foreach (var moles in gasAbsorberComp.AbsorbedMoles.Values)
            totalMoles += moles;

        return totalMoles > Min && totalMoles < Max;
    }
}
