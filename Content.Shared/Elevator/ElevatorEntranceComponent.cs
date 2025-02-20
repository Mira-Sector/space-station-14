using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Elevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElevatorEntranceComponent : Component
{
    [DataField(required: true)]
    public string ElevatorMapKey = string.Empty;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? ElevatorMap;

    [DataField(required: true)]
    public string EntranceId = string.Empty;

    [ViewVariables, AutoNetworkedField]
    public MapId? StartingMap;

    [DataField(required: true)]
    public string ExitId = string.Empty;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Exit;
}
