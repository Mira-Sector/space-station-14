using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Surgery;

[DataDefinition, Serializable]
public partial class SurgeryGraph
{
    public const string StartingNode = "start";

    /// <summary>
    /// List of nodes this graph contains
    /// </summary>
    [DataField]
    public List<SurgeryNode> Nodes { get; set; } = new();

    public bool TryFindNode(int? hashCode, [NotNullWhen(true)] out SurgeryNode? targetNode)
    {
        targetNode = null;

        foreach (var node in Nodes)
        {
            if (node.GetHashCode() != hashCode)
                continue;

            targetNode = node;
            return true;
        }

        return false;
    }

    public bool TryFindNode(string nodeId, [NotNullWhen(true)] out SurgeryNode? targetNode)
    {
        targetNode = null;

        foreach (var node in Nodes)
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
        return TryFindNode(SurgeryGraph.StartingNode, out start);
    }
}
