using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Elevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElevatorStationComponent : Component
{
    [DataField(required: true)]
    public Dictionary<string, ResPath> ElevatorMapPaths = new();

    [ViewVariables, AutoNetworkedField]
    public Dictionary<string, NetEntity> ElevatorMaps = new();
}
