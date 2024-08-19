using Robust.Shared.GameStates;

namespace Content.Shared.Vehicles;

[RegisterComponent, NetworkedComponent]
public sealed partial class VehicleComponent : Component
{
    [DataField]
    public float Speed = 1f;

    [DataField]
    public EntityUid? Driver;
}

