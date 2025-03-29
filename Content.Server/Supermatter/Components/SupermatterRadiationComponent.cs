namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterRadiationComponent : Component
{
    [DataField]
    public float IntensityBase;

    [DataField]
    public float SlopeBase;

    [ViewVariables]
    public float Intensity;

    [ViewVariables]
    public float Slope;
}
