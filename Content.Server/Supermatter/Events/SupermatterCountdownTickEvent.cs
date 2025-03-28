namespace Content.Server.Supermatter.Events;

public sealed partial class SupermatterCountdownTickEvent : EntityEventArgs
{
    public TimeSpan ElapsedTime;
    public TimeSpan Timer;

    public SupermatterCountdownTickEvent(TimeSpan elapsedTime, TimeSpan timer)
    {
        ElapsedTime = elapsedTime;
        Timer = timer;
    }
}
