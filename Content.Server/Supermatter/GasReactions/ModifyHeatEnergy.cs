using Content.Server.Supermatter.Components;
using Content.Shared.Atmos;

namespace Content.Server.Supermatter.GasReactions;

[DataDefinition]
public sealed partial class ModifyHeatEnergy : SupermatterGasReaction
{
    [DataField]
    public float Multiplier;

    public override bool React(EntityUid supermatter, Gas? gas, GasMixture air, TimeSpan lastReaction, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<SupermatterHeatEnergyComponent>(supermatter, out var heatEnergyComp))
            return false;

        var percentage = gas == null ? 1f : air.GetMoles(gas.Value) / air.TotalMoles;
        var supermatterSys = entMan.System<SupermatterSystem>();
        supermatterSys.ModifyHeatEnergy((supermatter, heatEnergyComp), heatEnergyComp.BaseEnergy * percentage * Multiplier * (float) lastReaction.TotalSeconds);
        return true;
    }
}
