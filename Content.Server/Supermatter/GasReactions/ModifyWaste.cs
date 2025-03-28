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

        if (gas != null && gasEmitterComp.PreviousPercentage.TryGetValue(gas.Value, out var previousPercentage))
            gasEmitterComp.CurrentRate -= previousPercentage * Multiplier * (float) lastReaction.TotalSeconds;

        var percentage = gas == null ? 1f : air.GetMoles(gas.Value) / air.TotalMoles;

        gasEmitterComp.CurrentRate += percentage * Multiplier * (float) lastReaction.TotalSeconds;

        if (gas == null)
            return true;

        if (gasEmitterComp.PreviousPercentage.ContainsKey(gas.Value))
        {
            gasEmitterComp.PreviousPercentage[gas.Value] = percentage;
        }
        else
        {
            gasEmitterComp.PreviousPercentage.Add(gas.Value, percentage);
        }

        return true;
    }
}
