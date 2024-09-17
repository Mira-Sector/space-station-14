namespace Content.Server.Footprints.Components;

[RegisterComponent]
public sealed partial class GivesFootprintsComponent : Component
{
    [DataField(required: true)]
    public string? Container;
}
