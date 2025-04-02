using Content.Shared.Atmos;

namespace Content.Server.Supermatter.Events;

public sealed partial class SupermatterGasAbsorbedEvent : EntityEventArgs
{
    public Dictionary<Gas, float> AbsorbedMoles;
    public float TotalMoles;

    public SupermatterGasAbsorbedEvent(Dictionary<Gas, float> absorbedMoles, float totalMoles)
    {
        AbsorbedMoles = absorbedMoles;
        TotalMoles = totalMoles;
    }
}
