using Robust.Shared.GameStates;

namespace Content.Shared.Elevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElevatorCollisionComponent : Component
{
    [DataField(required: true)]
    public string CollisionId = string.Empty;

    [ViewVariables, AutoNetworkedField]
    public HashSet<NetEntity> Collided = new();
}
