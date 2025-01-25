using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.IO;

namespace Content.Shared.Surgery;

[Prototype]
public sealed partial class SurgeryPrototype : SurgeryGraph, IPrototype, ISerializationHooks
{
    [IdDataField]
    public string ID { get; } = default!;

    void ISerializationHooks.AfterDeserialization()
    {
        HashSet<string> nodeIds = new();
        foreach (var node in Nodes)
        {
            if (node.Id != null)
            {
                if (nodeIds.Contains(node.Id))
                    throw new InvalidDataException($"Duplicate node ID {node.Id} in surgery graph {ID}");

                nodeIds.Add(node.Id);
            }

            foreach (var edge in node.Edges)
            {
                if (edge._connection is not {})
                    continue;

                if (!TryFindNode(edge._connection, out var connection))
                    throw new InvalidDataException($"Cannot find node {edge._connection} in surgery graph {ID}");

                edge.Connection = connection;
                connection.Connections.Add(edge);
            }
        }
    }
}
