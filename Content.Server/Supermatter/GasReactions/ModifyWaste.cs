using Content.Server.Supermatter.Components;
using Content.Shared.Atmos;

namespace Content.Server.Supermatter.GasReactions;

[DataDefinition]
public sealed partial class ModifyWaste : SupermatterGasReaction
{
    [DataField]
    public float Multiplier;

    public override bool React(EntityUid supermatter, Gas? gas, GasMixture air, TimeSpan lastReaction, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<SupermatterGasEmitterComponent>(supermatter, out var gasEmitterComp))
            return false;

        var percentage = gas == null ? 1f : air.GetMoles(gas.Value) / air.TotalMoles;
        gasEmitterComp.CurrentRate += gasEmitterComp.BaseRate * percentage * Multiplier * (float) lastReaction.TotalSeconds;
        return true;
    }
}
