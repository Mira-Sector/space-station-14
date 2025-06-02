namespace Content.Server.Supermatter.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SupermatterDelaminationTeleportMapReturnComponent: Component
{
    [DataField]
    public TimeSpan Delay;

    [ViewVariables, AutoPausedField]
    public TimeSpan NextTeleport;
}
