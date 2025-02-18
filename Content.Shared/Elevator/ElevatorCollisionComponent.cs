using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Elevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElevatorCollisionComponent : Component
{
    [DataField(required: true)]
    public string CollisionId = string.Empty;

    [ViewVariables, AutoNetworkedField]
    public HashSet<NetEntity> Collided = new();

    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "ElevatorFloorChange";
}
