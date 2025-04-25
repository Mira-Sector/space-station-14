using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Surgery;

[DataDefinition, Virtual, Serializable, NetSerializable]
public partial class SurgeryGraph
{
    [DataField("nodes")]
    public List<SurgeryNode> _nodes { get; set; } = new();

    /// <summary>
    /// List of nodes this graph contains
    /// </summary>
    [ViewVariables]
    public Dictionary<int, SurgeryNode> Nodes { get; set; } = new();

    [DataField("startingNode", required: true)]
    public string _startingNode = default!;

    [ViewVariables]
    public int StartingNode;

    public int GetNextId()
    {
        var largest = 0;

        foreach (var (id, _) in Nodes)
        {
            if (id > largest)
                largest = id;
        }

        return largest + 1;
    }

    public bool TryFindNode(int? targetId, [NotNullWhen(true)] out SurgeryNode? targetNode)
    {
        targetNode = null;

        foreach (var (nodeId, node) in Nodes)
        {
            if (nodeId != targetId)
                continue;

            targetNode = node;
            return true;
        }

        return false;
    }

    public bool TryFindNodeId(SurgeryNode? targetNode, [NotNullWhen(true)] out int? targetNodeId)
    {
        targetNodeId = null;

        foreach (var (nodeId, node) in Nodes)
        {
            if (node == targetNode)
            {
                targetNodeId = nodeId;
                return true;
            }
        }

        return false;
    }

    public bool TryFindNode(string nodeId, [NotNullWhen(true)] out SurgeryNode? targetNode)
    {
        targetNode = null;

        foreach (var node in _nodes)
        {
            if (node.Id != nodeId)
                continue;

            targetNode = node;
            return true;
        }

        return false;
    }

    public bool TryGetStaringNode([NotNullWhen(true)] out SurgeryNode? start)
    {
        return TryFindNode(StartingNode, out start);
    }
}
