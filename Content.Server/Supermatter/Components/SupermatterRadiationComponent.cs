namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterRadiationComponent : Component
{
    [DataField]
    public float IntensityPower;

    [ViewVariables]
    public float Intensity;
}
