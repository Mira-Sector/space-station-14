using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Surgery;

[Serializable]
[DataDefinition]
public sealed partial class SurgeryGraphNode
{
    [DataField("node", required: true)]
    public string Name { get; private set; } = default!;

    [DataField("actions", serverOnly: true)]
    public ISurgeryAction[] _actions = Array.Empty<ISurgeryAction>();

    [DataField("edges")]
    public SurgeryGraphEdge[] _edges = Array.Empty<SurgeryGraphEdge>();

    [ViewVariables]
    public IReadOnlyList<SurgeryGraphEdge> Edges => _edges;

    [ViewVariables]
    public IReadOnlyList<ISurgeryAction> Actions => _actions;

    public SurgeryGraphEdge? GetEdge(string target)
    {
        foreach (var edge in _edges)
        {
            if (edge.Target == target)
                return edge;
        }

        return null;
    }

    public int? GetEdgeIndex(string target)
    {
        for (var i = 0; i < _edges.Length; i++)
        {
            var edge = _edges[i];
            if (edge.Target == target)
                return i;
        }

        return null;
    }

    public bool TryGetEdge(string target, [NotNullWhen(true)] out SurgeryGraphEdge? edge)
    {
        return (edge = GetEdge(target)) != null;
    }
}
