using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.Stage;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RacerArcadeStageEdgeStage : IRacerArcadeStageEdge
{
    [DataField(required: true)]
    public ProtoId<RacerGameStagePrototype> StageId;
}
