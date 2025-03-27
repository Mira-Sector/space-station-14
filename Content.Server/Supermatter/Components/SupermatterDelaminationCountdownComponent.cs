namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterDelaminationCountdownComponent : Component
{
    [DataField]
    public TimeSpan Length;

    [ViewVariables]
    public TimeSpan ElapsedTime;

    [ViewVariables]
    public bool Active;

    [DataField]
    public TimeSpan TickDelay;

    [ViewVariables]
    public TimeSpan NextTick;
}
