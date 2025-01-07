namespace Content.Server.Silicons.StationAi;

[RegisterComponent]
public sealed partial class StationAiRequirePowerComponent : Component
{
    [ViewVariables]
    public bool IsPowered = true;

    [DataField]
    public float Wattage = 10f;
}
