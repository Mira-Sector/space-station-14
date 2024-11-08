using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Wounds;

[Serializable]
[DataDefinition]
public sealed partial class WoundGraphNode
{
    [DataField("node", required: true)]
    public string Name { get; private set; } = default!;

    [DataField("actions", serverOnly: true)]
    public IWoundAction[] _actions = Array.Empty<IWoundAction>();

    [DataField("edges")]
    private WoundGraphEdge[] _edges = Array.Empty<WoundGraphEdge>();

    [ViewVariables]
    public IReadOnlyList<WoundGraphEdge> Edges => _edges;

    [ViewVariables]
    public IReadOnlyList<IWoundAction> Actions => _actions;

    public WoundGraphEdge? GetEdge(string target)
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

    public bool TryGetEdge(string target, [NotNullWhen(true)] out WoundGraphEdge? edge)
    {
        return (edge = GetEdge(target)) != null;
    }
}
