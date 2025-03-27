namespace Content.Server.Supermatter.Events;

public sealed partial class SupermatterCountdownTickEvent : EntityEventArgs
{
    public TimeSpan ElapsedTime;

    public SupermatterCountdownTickEvent(TimeSpan elapsedTime)
    {
        ElapsedTime = elapsedTime;
    }
}
