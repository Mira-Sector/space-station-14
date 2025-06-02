namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterEnergyArcShooterComponent : Component
{
    [ViewVariables]
    public int Arcs;

    [DataField]
    public float EnergyRequiredForArc;

    [DataField]
    public float DelayScale;

    [ViewVariables]
    public float MinDelay;

    [ViewVariables]
    public float MaxDelay;
}
