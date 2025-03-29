namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterEnergyArcShooterComponent : Component
{
    [ViewVariables]
    public float MinInterval;

    [ViewVariables]
    public float MaxInterval;
}
