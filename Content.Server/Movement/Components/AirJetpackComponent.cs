namespace Content.Server.Movement.Components;

[RegisterComponent]
public sealed partial class AirJetpackComponent : Component
{
    [DataField]
    public float MoleUsage = 0.012f;
}
