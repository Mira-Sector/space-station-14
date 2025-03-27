namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterEnergyArcShooterComponent : Component
{
    [DataField]
    public float DelayPerEnergy;

    [ViewVariables]
    public float MinInterval;

    [ViewVariables]
    public float MaxInterval;
}
