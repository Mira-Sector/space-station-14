namespace Content.Server.Body.Components;

[RegisterComponent]
public sealed partial class VomitOnRotComponent : Component
{
    [DataField]
    public float HealthyChance = 0f;

    [DataField]
    public float DamagedChance = 0.9f;

    [ViewVariables]
    public float CurrentChance;
}
