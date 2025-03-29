using Content.Server.Supermatter.GasReactions;
using Content.Shared.Atmos;

namespace Content.Server.Supermatter.Events;

public sealed partial class SupermatterGasReactedEvent : EntityEventArgs
{
    public Dictionary<Gas, HashSet<SupermatterGasReaction>> Reactions = new();

    public SupermatterGasReactedEvent(Dictionary<Gas, HashSet<SupermatterGasReaction>> reactions)
    {
        Reactions = reactions;
    }
}
