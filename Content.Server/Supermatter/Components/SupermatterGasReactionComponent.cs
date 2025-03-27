using Content.Shared.Atmos;

namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterGasReactionComponent : Component
{
    [DataField]
    public Dictionary<Gas, SupermatterGasReaction> GasReactions = new();
}
