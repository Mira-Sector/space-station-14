using Content.Shared.Arcade.Racer.Objects;
using Content.Shared.Arcade.Racer.Objects.Vehicles;
using Content.Shared.Arcade.Racer.Stage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer;

[Serializable, NetSerializable]
public sealed partial class RacerGameState
{
    [ViewVariables]
    public required ProtoId<RacerGameStagePrototype> CurrentStage;

    [ViewVariables]
    public required RacerArcadeStageNode CurrentNode;

    [ViewVariables]
    public required List<BaseRacerGameObject> Objects;

    [ViewVariables]
    public required RacerGameVehiclePlayer Player;
}
