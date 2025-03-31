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

    /// <summary>
    /// Can the timer tick down
    /// </summary>
    [ViewVariables]
    public bool Enabled;

    [DataField]
    public TimeSpan TickDelay;

    [ViewVariables]
    [AutoPausedField]
    public TimeSpan NextTick;
}
