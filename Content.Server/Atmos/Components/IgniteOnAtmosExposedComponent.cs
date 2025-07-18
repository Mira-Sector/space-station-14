using Content.Shared.Atmos;

namespace Content.Server.Atmos.Components;

[RegisterComponent]
public sealed partial class IgniteOnAtmosExposedComponent : Component
{
    [DataField]
    public bool Additive = false;

    [DataField(required: true)]
    public float FireStacks { get; set; }

    [DataField(required: true)]
    public GasMixture Gas = new();
}
