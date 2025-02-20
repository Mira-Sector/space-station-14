using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Elevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElevatorExitComponent : Component
{
    [DataField(required: true)]
    public string ExitId = string.Empty;

    [ViewVariables, AutoNetworkedField]
    public MapId? StartingMap;
}
