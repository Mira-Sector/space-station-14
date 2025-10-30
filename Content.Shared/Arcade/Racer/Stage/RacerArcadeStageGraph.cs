using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.Stage;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RacerArcadeStageGraph
{
    [DataField(required: true)]
    public Dictionary<string, RacerArcadeStageNode> Nodes = [];

    [DataField(required: true)]
    public string StartingNode = default!;
}
