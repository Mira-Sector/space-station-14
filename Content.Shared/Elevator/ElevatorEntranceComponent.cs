using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Elevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElevatorEntranceComponent : Component
{
    [DataField(required: true)]
    public string ElevatorMapKey = string.Empty;

    [ViewVariables, AutoNetworkedField]
    public MapId? ElevatorMap;

    [DataField(required: true)]
    public string EntranceId = string.Empty;

    [ViewVariables, AutoNetworkedField]
    public MapId? StartingMap;

    [DataField(required: true)]
    public string ExitId = string.Empty;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Exit;

    [DataField]
    public TimeSpan? Delay = TimeSpan.FromSeconds(4f);

    [ViewVariables, AutoNetworkedField]
    public TimeSpan? NextTeleport;

    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid>? NextTeleportEntities;

    [DataField]
    public ProtoId<SourcePortPrototype> DelayPort = "ElevatorEntranceDelayed";

    [DataField]
    public ProtoId<SourcePortPrototype> FinishedPort = "ElevatorEntranceFinished";
}
