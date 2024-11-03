namespace Content.Server.Footprints.Components;

[RegisterComponent]
public sealed partial class RemoveFootprintsComponent : Component
{
    [DataField]
    public bool Enabled = true;
}
