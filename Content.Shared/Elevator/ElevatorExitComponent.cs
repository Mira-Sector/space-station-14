using Robust.Shared.GameStates;

namespace Content.Shared.Elevator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ElevatorExitComponent : Component
{
    [DataField(required: true)]
    public string ExitId = string.Empty;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? StartingMap;
}
