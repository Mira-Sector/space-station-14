namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterEnergyDecayComponent : Component
{
    [DataField]
    public TimeSpan Delay;

    [ViewVariables]
    public TimeSpan NextDecay;

    [DataField]
    public float EnergyDecay;
}
