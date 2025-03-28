using Content.Server.Supermatter.Components;
using Content.Shared.Atmos;

namespace Content.Server.Supermatter.GasReactions;

[DataDefinition]
public sealed partial class HeatEnergyGain : SupermatterGasReaction
{
    [DataField]
    public float Multiplier;

    public override bool React(EntityUid supermatter, Gas? gas, GasMixture air, TimeSpan lastReaction, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<SupermatterEnergyHeatGainComponent>(supermatter, out var heatGainComponent))
            return false;

        var supermatterSys = entMan.System<SupermatterSystem>();

        var energy = heatGainComponent.CurrentGain + Multiplier * (float) lastReaction.TotalSeconds;
        supermatterSys.ModifyEnergy(supermatter, heatGainComponent.CurrentGain - energy);
        heatGainComponent.CurrentGain += energy;
        return true;
    }
}
