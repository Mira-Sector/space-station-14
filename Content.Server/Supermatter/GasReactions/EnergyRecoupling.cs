using Content.Server.Supermatter.Components;
using Content.Shared.Atmos;

namespace Content.Server.Supermatter.GasReactions;

[DataDefinition]
public sealed partial class EnergyRecoupling : SupermatterGasReaction
{
    /// <summary>
    /// What to use in the case this is space
    /// </summary>
    [DataField]
    public float Energy;

    public override bool React(EntityUid supermatter, Gas? gas, GasMixture air, TimeSpan lastReaction, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<SupermatterEnergyDecayComponent>(supermatter, out var decayComp))
            return false;

        var energy = gas == null ? Energy : (air.GetMoles(gas.Value) / air.TotalMoles) * decayComp.LastLostEnergy;
        var supermatterSys = entMan.System<SupermatterSystem>();
        supermatterSys.ModifyEnergy(supermatter, energy);
        return true;
    }
}
