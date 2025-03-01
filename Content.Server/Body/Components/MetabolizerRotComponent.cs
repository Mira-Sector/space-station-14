namespace Content.Server.Body.Components;

[RegisterComponent]
public sealed partial class MetabolizerRotComponent : Component
{
    [DataField]
    public float HealthyMultiplier = 1f;

    [DataField]
    public float DamagedMultiplier = 2.25f;

    [ViewVariables]
    public float CurrentMutliplier;

    [DataField]
    public bool DisabledOnRot = true;

    [ViewVariables]
    public bool Enabled;
}
