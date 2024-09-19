namespace Content.Server.Objectives.Components;

[RegisterComponent]
public sealed partial class PickObsessionDepartmentComponent : Component
{
    [DataField(required: true)]
    public string Title = string.Empty;

    [DataField]
    public string? Description;
}
