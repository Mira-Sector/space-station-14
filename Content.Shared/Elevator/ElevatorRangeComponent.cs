using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Shared.Elevator;

[RegisterComponent, NetworkedComponent]
public sealed partial class ElevatorRangeComponent : Component
{
    [DataField(required: true)]
    public float Range;

    [DataField]
    public Vector2 Offset;

    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "ElevatorFloorChange";
}
