namespace Content.Server.Supermatter.Events;

public sealed partial class SupermatterEnergyModifiedEvent : EntityEventArgs
{
    public float CurrentEnergy;
    public float PreviousEnergy;

    public SupermatterEnergyModifiedEvent(float currentEnergy, float previousEnergy)
    {
        CurrentEnergy = currentEnergy;
        PreviousEnergy = previousEnergy;
    }
}
