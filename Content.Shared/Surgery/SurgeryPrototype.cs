using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Surgery;

[Prototype]
public sealed partial class SurgeryPrototype : SurgeryGraph, IPrototype, ISerializationHooks
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// The nodes that lead from the <see cref=SurgeryGraph.StartingNode> to this current node.
    /// </summary>
    /// <remarks>
    /// An empty list means we are the <see cref=SurgeryGraph.StartingNode>
    /// </remarks>
    [ViewVariables]
    public Dictionary<string, HashSet<SurgeryNode>?> PreviousNodes = new();

    void ISerializationHooks.AfterDeserialization()
    {
        foreach (var node in Nodes)
        {
            PreviousNodes.Add(node.ID, FindPreviousNodes(node));
        }
    }

    HashSet<SurgeryNode> FindPreviousNodes(SurgeryNode currentNode)
    {
        HashSet<SurgeryNode> nodes = new();
        Stack<SurgeryNode> stack = new();
        stack.Append(currentNode);

        while (true)
        {
            if (!FindPreviousNode(stack.Pop(), out var newNode))
                break;

            // circular node
            if (!nodes.Add(newNode))
                break;

            stack.Push(newNode);
        }

        return nodes;
    }

    bool FindPreviousNode(SurgeryNode currentNode, [NotNullWhen(true)] out SurgeryNode? previousNode)
    {
        previousNode = null;

        foreach (var node in Nodes)
        {
            foreach (var edge in node.Edges)
            {
                if (edge.Connection != currentNode.ID)
                    continue;

                previousNode = node;
                return true;
            }
        }

        return false;
    }
}
