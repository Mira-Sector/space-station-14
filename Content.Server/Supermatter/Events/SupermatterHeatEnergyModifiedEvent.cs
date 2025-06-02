namespace Content.Server.Supermatter.Events;

public sealed partial class SupermatterHeatEnergyModifiedEvent : EntityEventArgs
{
    public float CurrentHeatEnergy;
    public float PreviousHeatEnergy;

    public SupermatterHeatEnergyModifiedEvent(float currentHeatEnergy, float previousHeatEnergy)
    {
        CurrentHeatEnergy = currentHeatEnergy;
        PreviousHeatEnergy = previousHeatEnergy;
    }
}
