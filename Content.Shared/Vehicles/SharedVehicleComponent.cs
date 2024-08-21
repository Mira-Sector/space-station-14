using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicles;

[RegisterComponent, NetworkedComponent]
public sealed partial class VehicleComponent : Component
{
    [DataField]
    public EntityUid? Driver;

    [DataField]
    public int RequiredHands = 1;
}
[Serializable, NetSerializable]
public enum VehicleState
{
    Animated
}
