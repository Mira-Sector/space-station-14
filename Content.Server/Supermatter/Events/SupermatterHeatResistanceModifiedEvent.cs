namespace Content.Server.Supermatter.Events;

public sealed partial class SupermatterHeatResistanceModifiedEvent : EntityEventArgs
{
    public float CurrentHeatResistance;
    public float PreviousHeatResistance;

    public SupermatterHeatResistanceModifiedEvent(float currentHeatResistance, float previousHeatResistance)
    {
        CurrentHeatResistance = currentHeatResistance;
        PreviousHeatResistance = previousHeatResistance;
    }
}
