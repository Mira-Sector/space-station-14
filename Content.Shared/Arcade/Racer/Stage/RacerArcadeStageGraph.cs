using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.Stage;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RacerArcadeStageGraph
{
    [DataField(required: true)]
    public Dictionary<string, RacerArcadeStageNode> Nodes = [];

    [DataField(required: true)]
    public string? StartingNode = null;

    public bool TryGetStartingNode([NotNullWhen(true)] out RacerArcadeStageNode? node)
    {
        if (StartingNode is not { } starting)
        {
            node = null;
            return false;
        }

        return Nodes.TryGetValue(starting, out node);
    }

    public bool TryGetNode(string nodeId, [NotNullWhen(true)] out RacerArcadeStageNode? node)
    {
        return Nodes.TryGetValue(nodeId, out node);
    }
}
