using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Elevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElevatorCollisionComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> Collided = [];

    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "ElevatorFloorChange";
}
