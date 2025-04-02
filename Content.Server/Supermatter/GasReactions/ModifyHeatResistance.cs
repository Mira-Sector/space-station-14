using Content.Server.Supermatter.Components;
using Content.Shared.Atmos;

namespace Content.Server.Supermatter.GasReactions;

[DataDefinition]
public sealed partial class ModifyHeatResistance : SupermatterGasReaction
{
    [DataField]
    public float Multiplier;

    public override bool React(EntityUid supermatter, Gas? gas, GasMixture air, TimeSpan lastReaction, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<SupermatterHeatResistanceComponent>(supermatter, out var heatResistanceComp))
            return false;

        var multiplier = Multiplier;

        if (gas != null)
            multiplier *= air.GetMoles(gas.Value) / air.TotalMoles;

        multiplier *= (float) lastReaction.TotalSeconds;

        var supermatterSys = entMan.System<SupermatterSystem>();
        supermatterSys.ModifyHeatResistance((supermatter, heatResistanceComp), heatResistanceComp.BaseHeatResistance * multiplier);
        return true;
    }
}
