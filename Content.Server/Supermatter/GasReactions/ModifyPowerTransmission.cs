using Content.Server.Supermatter.Components;
using Content.Shared.Atmos;

namespace Content.Server.Supermatter.GasReactions;

[DataDefinition]
public sealed partial class ModifyPowerTransmission : SupermatterGasReaction
{
    /// <summary>
    /// What to use in the case this is space
    /// </summary>
    [DataField]
    public float Power;

    public override bool React(EntityUid supermatter, Gas? gas, GasMixture air, TimeSpan lastReaction, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<SupermatterPowerTransmissionComponent>(supermatter, out var powerComp))
            return false;

        var power = gas == null ? Power : (air.GetMoles(gas.Value) / air.TotalMoles);
        power *= (float) lastReaction.TotalSeconds;
        var supermatterSys = entMan.System<SupermatterSystem>();
        supermatterSys.ModifyPowerTransmission((supermatter, powerComp), power);
        return true;
    }
}
