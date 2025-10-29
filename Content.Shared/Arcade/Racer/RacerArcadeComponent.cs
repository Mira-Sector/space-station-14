using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Arcade.Racer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RacerArcadeComponent : Component
{
    [DataField(required: true)]
    public ProtoId<RacerGameStagePrototype> StartingStage;

    [ViewVariables, AutoNetworkedField]
    public RacerGameState State;
}
