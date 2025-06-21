namespace Content.Server.Body.Components;

[RegisterComponent]
public sealed partial class DisableRespireOnItemToggleComponent : Component
{
    [DataField]
    public bool DisableOnEnable;
}
