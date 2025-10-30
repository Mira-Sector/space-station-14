using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.Stage;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RacerArcadeStageEdgeNode : IRacerArcadeStageEdge
{
    [DataField(required: true)]
    public string ConnectionId;
}
