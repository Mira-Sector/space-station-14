using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JetpackComponent : Component
{
    [DataField]
    public EntProtoId? ToggleAction = "ActionToggleJetpack";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;
}
