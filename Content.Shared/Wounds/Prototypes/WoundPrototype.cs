using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Content.Shared.Wounds.Prototypes;

[Prototype]
public sealed partial class WoundPrototype : IPrototype, ISerializationHooks
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("graph", required: true)]
    public List<WoundGraphNode> _graph = new();

    [DataField]
    public string? Start { get; private set; }

    [ViewVariables]
    public IReadOnlyDictionary<string, WoundGraphNode> Nodes => _nodes;

    private readonly Dictionary<string, WoundGraphNode> _nodes = new();
    private readonly Dictionary<(string, string), WoundGraphNode[]?> _paths = new();
    private readonly Dictionary<string, Dictionary<WoundGraphNode, WoundGraphNode?>> _pathfinding = new();

    void ISerializationHooks.AfterDeserialization()
    {
        _nodes.Clear();

        foreach (var graphNode in _graph)
        {
            if (string.IsNullOrEmpty(graphNode.Name))
            {
                throw new InvalidDataException($"{ID} name not set in wound graph!");
            }

            _nodes[graphNode.Name] = graphNode;
        }

        if (string.IsNullOrEmpty(Start) || !_nodes.ContainsKey(Start))
            throw new InvalidDataException($"Starting wound node {ID} is null, empty or invalid!");
    }

    public WoundGraphEdge? Edge(string startNode, string nextNode)
    {
        var start = _nodes[startNode];
        return start.GetEdge(nextNode);
    }

    public bool TryPath(string startNode, string finishNode, [NotNullWhen(true)] out WoundGraphNode[]? path)
    {
        return (path = Path(startNode, finishNode)) != null;
    }

    public string[]? PathId(string startNode, string finishNode)
    {
        if (Path(startNode, finishNode) is not {} path)
            return null;

        var nodes = new string[path.Length];

        for (var i = 0; i < path.Length; i++)
        {
            nodes[i] = path[i].Name;
        }

        return nodes;
    }

    public WoundGraphNode[]? Path(string startNode, string finishNode)
    {
        var tuple = (startNode, finishNode);

        if (_paths.ContainsKey(tuple))
            return _paths[tuple];

        Dictionary<WoundGraphNode, WoundGraphNode?> pathfindingForStart;
        if (_pathfinding.ContainsKey(startNode))
        {
            pathfindingForStart = _pathfinding[startNode];
        }
        else
        {
            pathfindingForStart = _pathfinding[startNode] = PathsForStart(startNode);
        }

        // Follow the chain backwards.
        var start = _nodes[startNode];
        var finish = _nodes[finishNode];
        var current = finish;
        var path = new List<WoundGraphNode>();

        while (current != start)
        {
            // No path.
            if (current == null || !pathfindingForStart.ContainsKey(current))
            {
                // We remember this for next time.
                _paths[tuple] = null;
                return null;
            }
            path.Add(current);
            current = pathfindingForStart[current];
        }

        path.Reverse();
        return _paths[tuple] = path.ToArray();
    }

    private Dictionary<WoundGraphNode, WoundGraphNode?> PathsForStart(string start)
    {
        var startNode = _nodes[start];
        var frontier = new Queue<WoundGraphNode>();
        var cameFrom = new Dictionary<WoundGraphNode, WoundGraphNode?>();

        frontier.Enqueue(startNode);
        cameFrom[startNode] = null;

        while (frontier.Count != 0)
        {
            var current = frontier.Dequeue();

            foreach (var edge in current.Edges)
            {
                var edgeNode = _nodes[edge.Target];
                if(cameFrom.ContainsKey(edgeNode)) continue;
                frontier.Enqueue(edgeNode);
                cameFrom[edgeNode] = current;
            }
        }

        return cameFrom;
    }
}
