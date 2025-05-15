using Content.Shared.Atmos;

namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterGasAbsorberComponent : Component
{
    [DataField]
    public float AbsorbedMultiplier;

    [ViewVariables]
    public Dictionary<Gas, float> AbsorbedMoles = new();

    [ViewVariables]
    public float TotalMoles;
}
