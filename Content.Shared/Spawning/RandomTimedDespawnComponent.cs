using Robust.Shared.GameStates;

namespace Content.Shared.Spawning;

[RegisterComponent, NetworkedComponent]
public sealed partial class RandomTimedDespawnComponent : Component
{
    [DataField(required: true)]
    public float Min;

    [DataField(required: true)]
    public float Max;
}
