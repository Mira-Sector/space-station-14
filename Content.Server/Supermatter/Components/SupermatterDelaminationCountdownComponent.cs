namespace Content.Server.Supermatter.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SupermatterDelaminationCountdownComponent : Component
{
    [DataField]
    public TimeSpan Length;

    [ViewVariables]
    [AutoPausedField]
    public TimeSpan ElapsedTime;

    /// <summary>
    /// Is there a timer counting down
    /// </summary>
    [ViewVariables]
    public bool Active;

    [DataField]
    public TimeSpan TickDelay;

    [ViewVariables]
    [AutoPausedField]
    public TimeSpan NextTick;
}
