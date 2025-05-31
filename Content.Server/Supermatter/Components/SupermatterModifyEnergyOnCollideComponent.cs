namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterModifyEnergyOnCollideComponent : Component
{
    [DataField]
    public float Scale = 1f;

    [DataField]
    public float Additional = 0f;
}
