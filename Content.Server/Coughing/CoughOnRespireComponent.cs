namespace Content.Server.Coughing;

[RegisterComponent]
public sealed partial class CoughOnRespireComponent : Component
{
    [DataField]
    public float Chance = 0.005f;
}
