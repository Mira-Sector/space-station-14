using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.IO;

namespace Content.Shared.Surgery.Prototypes;

[Prototype]
public sealed partial class SurgeryPrototype : SurgeryGraph, IPrototype, ISerializationHooks
{
    [IdDataField]
    public string ID { get; } = default!;

    void ISerializationHooks.AfterDeserialization()
    {
        _nodes.Clear();

        foreach (var graphNode in _graph)
        {
            if (string.IsNullOrEmpty(graphNode.Name))
            {
                throw new InvalidDataException($"{ID} name not set in surgery graph!");
            }

            _nodes[graphNode.Name] = graphNode;
        }

        if (string.IsNullOrEmpty(Start) || !_nodes.ContainsKey(Start))
            throw new InvalidDataException($"Starting surgery node {ID} is null, empty or invalid!");
    }

}
