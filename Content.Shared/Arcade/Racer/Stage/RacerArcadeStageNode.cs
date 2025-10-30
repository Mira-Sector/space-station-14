using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.Stage;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RacerArcadeStageNode
{
    [DataField]
    public List<IRacerArcadeStageEdge> Connections = [];
}
