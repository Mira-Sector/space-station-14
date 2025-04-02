using Content.Shared.Atmos;

namespace Content.Server.Supermatter.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SupermatterGasEmitterComponent : Component
{
    [DataField]
    public Dictionary<Gas, int> Ratios = new();

    [ViewVariables]
    public float CurrentRate;

    [DataField]
    public float BaseRate;

    [DataField]
    public float MinTemperature;

    [DataField]
    public float TemperaturePerRate;

    [ViewVariables]
    public float LastTemperature;

    [DataField]
    public TimeSpan Delay;

    [ViewVariables, AutoPausedField]
    public TimeSpan NextSpawn;
}
