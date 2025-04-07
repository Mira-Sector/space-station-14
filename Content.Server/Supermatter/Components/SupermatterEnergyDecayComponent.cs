namespace Content.Server.Supermatter.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SupermatterEnergyDecayComponent : Component
{
    [DataField]
    public TimeSpan Delay;

    [ViewVariables]
    [AutoPausedField]
    public TimeSpan NextDecay;

    [DataField]
    public float DecayPower;

    [DataField]
    public float DecayOffset;

    [ViewVariables]
    public float LastLostEnergy;
}
