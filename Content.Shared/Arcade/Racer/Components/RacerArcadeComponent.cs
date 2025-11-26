using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Arcade.Racer.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RacerArcadeComponent : Component
{
    [DataField(required: true)]
    public ProtoId<RacerGameStagePrototype> StartingStage;

    [DataField(required: true)]
    public EntProtoId PlayerShipId;

    [ViewVariables, AutoNetworkedField]
    public RacerGameState State;

    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> Players = [];
}
