using Content.Shared.Body.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedBodySystem))]
public sealed partial class BodyTrackerComponent : Component
{
    [ViewVariables]
    public Dictionary<string, Dictionary<EntityUid, IComponent>> Trackers = [];
}
