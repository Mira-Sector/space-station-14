using Robust.Shared.GameStates;

namespace Content.Shared.Vehicles;

[RegisterComponent, NetworkedComponent]
public sealed partial class VehicleComponent : Component
{
    [DataField]
    public EntityUid? Driver;

    [DataField]
    public float Speed = 1f;

    [DataField]
    public int RequiredHands = 1;
}

