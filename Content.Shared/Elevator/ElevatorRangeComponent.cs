using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Elevator;

[RegisterComponent, NetworkedComponent]
public sealed partial class ElevatorRangeComponent : Component
{
    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "ElevatorFloorChange";
}
