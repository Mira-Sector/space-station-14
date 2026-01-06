using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared.Elevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElevatorStationComponent : Component
{
    [DataField(required: true)]
    public Dictionary<string, ResPath> ElevatorMapPaths = new();

    [ViewVariables, AutoNetworkedField]
    public Dictionary<string, MapId> ElevatorMaps = new();
}
