using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery;

[Prototype]
public sealed partial class SurgeryPrototype : SurgeryGraph, IPrototype, ISerializationHooks
{
    [IdDataField]
    public string ID { get; } = default!;

    void ISerializationHooks.AfterDeserialization()
    {
        foreach (var node in Nodes)
        {
            foreach (var edge in node.Edges)
            {
                if (edge._connection is not {})
                    continue;

                if (!TryFindNode(edge._connection, out var connection))
                    continue;

                edge.Connection = connection;
                connection.Connections.Add(edge);
            }
        }
    }
}
