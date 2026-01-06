using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Elevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElevatorExitComponent : Component
{
    [DataField(required: true)]
    public string ExitId = string.Empty;

    [ViewVariables, AutoNetworkedField]
    public MapId? StartingMap;

    [DataField]
    public ProtoId<SourcePortPrototype> DelayPort = "ElevatorExitDelayed";

    [DataField]
    public ProtoId<SourcePortPrototype> FinishedPort = "ElevatorExitFinished";
}
