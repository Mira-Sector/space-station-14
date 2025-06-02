using Content.Server.Supermatter.GasReactions;
using Content.Shared.Atmos;

namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterGasReactionComponent : Component
{
    [DataField]
    public Dictionary<Gas, HashSet<SupermatterGasReaction>> GasReactions = new();

    [DataField]
    public HashSet<SupermatterGasReaction> SpaceReactions = new();

    [ViewVariables]
    public TimeSpan LastReaction;
}
