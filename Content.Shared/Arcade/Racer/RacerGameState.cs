using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer;

[Serializable, NetSerializable]
public sealed partial class RacerGameState(ProtoId<RacerGameStagePrototype> currentStage)
{
    [ViewVariables]
    public ProtoId<RacerGameStagePrototype> CurrentStage = currentStage;
}
