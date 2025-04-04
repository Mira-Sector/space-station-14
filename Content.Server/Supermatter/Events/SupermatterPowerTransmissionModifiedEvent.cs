namespace Content.Server.Supermatter.Events;

public sealed partial class SupermatterPowerTransmissionModifiedEvent : EntityEventArgs
{
    public float CurrentPower;
    public float PreviousPower;

    public SupermatterPowerTransmissionModifiedEvent(float currentPower, float previousPower)
    {
        CurrentPower = currentPower;
        PreviousPower = previousPower;
    }
}
