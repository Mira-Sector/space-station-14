using Content.Server.Supermatter.Components;
using Content.Server.Tesla.Components;
using Content.Shared.Atmos;

namespace Content.Server.Supermatter.GasReactions;

[DataDefinition]
public sealed partial class PowerTransmission : SupermatterGasReaction
{
    [DataField]
    public float Multiplier;

    [DataField]
    public float Additional;

    public override bool React(EntityUid supermatter, Gas? gas, GasMixture air, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<SupermatterEnergyArcShooterComponent>(supermatter, out var supermatterArcShooterComp))
            return false;

        var arcShooter = entMan.GetComponent<LightningArcShooterComponent>(supermatter);

        var power = 1f + Additional;

        if (gas != null)
        {
            power *= (air.GetMoles(gas.Value) / air.TotalMoles) * Multiplier;
        }
        else
        {
            power *= Multiplier;
        }

        arcShooter.ShootMinInterval -= supermatterArcShooterComp.MinInterval * power;
        arcShooter.ShootMaxInterval -= supermatterArcShooterComp.MinInterval * power;

        return true;
    }
}
