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
    public required List<NetEntity> Objects;
}
