using Content.Shared.Atmos;

namespace Content.Server.Supermatter.Events;

public sealed partial class SupermatterGasAbsorbedEvent : EntityEventArgs
{
    public Dictionary<Gas, float> AbsorbedMoles;
    public float TotalMoles;
    public TimeSpan LastReaction;

    public SupermatterGasAbsorbedEvent(Dictionary<Gas, float> absorbedMoles, float totalMoles, TimeSpan lastReaction)
    {
        AbsorbedMoles = absorbedMoles;
        TotalMoles = totalMoles;
        LastReaction = lastReaction;
    }
}
