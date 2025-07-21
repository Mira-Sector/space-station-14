using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.IO;

namespace Content.Shared.Surgery;

[Prototype]
public sealed partial class SurgeryPrototype : SurgeryGraph, IPrototype, ISerializationHooks
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Description;

    void ISerializationHooks.AfterDeserialization()
    {
        HashSet<string> nodeIds = [];

        foreach (var node in _nodes)
        {
            if (node.Id != null)
            {
                if (nodeIds.Contains(node.Id))
                    throw new InvalidDataException($"Duplicate node ID {node.Id} in surgery graph {ID}");

                nodeIds.Add(node.Id);
            }

            Nodes.Add(GetNextId(), node);
        }

        var startingNodeFound = false;

        foreach (var (nodeId, node) in Nodes)
        {
            foreach (var edge in node.Edges)
            {
                if (edge._connection is not { })
                    continue;

                if (!TryFindNode(edge._connection, out var connection))
                    throw new InvalidDataException($"Cannot find node {edge._connection} in surgery graph {ID}");

                if (!TryFindNodeId(connection, out var connectionId))
                    continue;

                edge.Connection = connectionId;
            }

            if (node.Id != _startingNode)
                continue;

            startingNodeFound = true;
            StartingNode = nodeId;
        }

        if (!startingNodeFound)
            throw new InvalidDataException($"Cannot find starting node {_startingNode} in surgery graph {ID}");
    }
}
