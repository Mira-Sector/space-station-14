using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Systems;

public abstract partial class SharedSurgerySystem
{
    // calculating the graph is expensive and reused multiple times so cache it
    private readonly Dictionary<HashSet<ProtoId<SurgeryPrototype>>, SurgeryGraph> _graphCache = [];

    public SurgeryGraph MergeGraphs(HashSet<ProtoId<SurgeryPrototype>> prototypeIds)
    {
        if (TryGetCachedGraph(prototypeIds, out var cached))
            return cached;

        var mergedGraph = BuildMergedGraph(prototypeIds);
        _graphCache[prototypeIds] = mergedGraph;
        return mergedGraph;
    }

    private bool TryGetCachedGraph(HashSet<ProtoId<SurgeryPrototype>> prototypeIds, out SurgeryGraph cached)
    {
        foreach (var (cachedIds, graph) in _graphCache)
        {
            if (cachedIds.SetEquals(prototypeIds))
            {
                cached = graph;
                return true;
            }
        }

        cached = null!;
        return false;
    }

    private SurgeryGraph BuildMergedGraph(HashSet<ProtoId<SurgeryPrototype>> prototypeIds)
    {
        var graph = new SurgeryGraph();

        foreach (var id in prototypeIds)
        {
            if (_prototype.TryIndex(id, out var proto))
                MergeGraphs(proto, graph);
        }

        return graph;
    }

    private static void MergeGraphs(SurgeryGraph sourceGraph, SurgeryGraph mergedGraph)
    {
        if (!sourceGraph.TryGetStaringNode(out var sourceStart))
            return;

        if (!mergedGraph.TryGetStaringNode(out var mergedStart))
        {
            CopyGraph(sourceGraph, mergedGraph, sourceStart);
            return;
        }

        HashSet<SurgeryNode> explored = [];
        explored.EnsureCapacity(sourceGraph.Nodes.Count);

        Queue<SurgeryNode> queue = [];
        queue.EnsureCapacity(sourceGraph.Nodes.Count);
        queue.Enqueue(sourceStart);

        Dictionary<SurgeryNode, SurgeryNode> nodeMap = [];
        nodeMap.EnsureCapacity(sourceGraph.Nodes.Count);
        nodeMap[sourceStart] = mergedStart;

        while (queue.TryDequeue(out var currentSource))
        {
            // node already explored
            if (!explored.Add(currentSource))
                continue;

            if (!nodeMap.TryGetValue(currentSource, out var currentMerged))
                continue;

            ProcessNodePair(currentSource, currentMerged, sourceGraph, mergedGraph, queue, nodeMap, explored);
        }
    }

    private static void ProcessNodePair(
        SurgeryNode sourceNode,
        SurgeryNode mergedNode,
        SurgeryGraph sourceGraph,
        SurgeryGraph mergedGraph,
        Queue<SurgeryNode> queue,
        Dictionary<SurgeryNode, SurgeryNode> nodeMap,
        HashSet<SurgeryNode> explored)
    {
        GetMatchingEdges(sourceNode, mergedNode, out var matches, out var missing);

        foreach (var (sourceEdge, mergedEdge) in matches)
        {
            if (!sourceGraph.TryFindNode(sourceEdge.Connection, out var nextSourceNode))
                continue;

            if (mergedGraph.TryFindNode(mergedEdge.Connection, out var nextMergedNode))
            {
                queue.Enqueue(nextSourceNode);
                nodeMap[nextSourceNode] = nextMergedNode;
            }
            else
            {
                ExpandGraph(sourceEdge, sourceNode, mergedNode, sourceGraph, mergedGraph, nodeMap, explored);
            }
        }

        foreach (var sourceEdge in missing)
            ExpandGraph(sourceEdge, sourceNode, mergedNode, sourceGraph, mergedGraph, nodeMap, explored);
    }

    private static void CopyGraph(SurgeryGraph sourceGraph, SurgeryGraph mergedGraph, SurgeryNode startNode)
    {
        var newStart = new SurgeryNode
        {
            Special = new(startNode.Special)
        };

        var newId = mergedGraph.GetNextId();
        mergedGraph.Nodes.Add(newId, newStart);
        mergedGraph.StartingNode = newId;

        HashSet<SurgeryNode> explored = [];
        explored.EnsureCapacity(sourceGraph.Nodes.Count);
        explored.Add(startNode);

        Dictionary<SurgeryNode, SurgeryNode> nodeMap = [];
        nodeMap.EnsureCapacity(sourceGraph.Nodes.Count);
        nodeMap[startNode] = newStart;

        foreach (var edge in startNode.Edges)
            ExpandGraph(edge, startNode, newStart, sourceGraph, mergedGraph, nodeMap, explored);
    }

    public static void GetMatchingEdges(SurgeryNode sourceNode, SurgeryNode mergedNode, out Dictionary<SurgeryEdge, SurgeryEdge> matching, out HashSet<SurgeryEdge> missing)
    {
        matching = [];
        missing = [];

        if (!sourceNode.Special.SetEquals(mergedNode.Special))
            return;

        foreach (var edge in sourceNode.Edges)
        {
            var match = mergedNode.Edges.FirstOrDefault(
                me => edge.Requirement.RequirementsMatch(me.Requirement, out _));

            if (match != null)
                matching[edge] = match;
            else
                missing.Add(edge);
        }
    }
    public static void ExpandGraph(
        SurgeryEdge sourceEdge,
        SurgeryNode sourceNode,
        SurgeryNode mergedNode,
        SurgeryGraph sourceGraph,
        SurgeryGraph mergedGraph,
        Dictionary<SurgeryNode, SurgeryNode> nodeMap,
        HashSet<SurgeryNode> explored)
    {
        if (!sourceGraph.TryFindNode(sourceEdge.Connection, out var sourceConnection))
            return;

        var newMergedEdge = new SurgeryEdge
        {
            Requirement = sourceEdge.Requirement
        };
        mergedNode.Edges.Add(newMergedEdge);

        Queue<SurgeryNode> toVisit = [];
        toVisit.EnsureCapacity(sourceGraph.Nodes.Count);
        Dictionary<SurgeryNode, SurgeryEdge> pendingEdges = [];
        pendingEdges.EnsureCapacity(sourceGraph.Nodes.Count);

        toVisit.Enqueue(sourceConnection);
        pendingEdges[sourceConnection] = newMergedEdge;

        TraverseAndCloneNodes(toVisit, pendingEdges, sourceGraph, mergedGraph, nodeMap, explored);
        ResolvePendingConnections(pendingEdges, nodeMap, mergedGraph);
    }

    private static void TraverseAndCloneNodes(
        Queue<SurgeryNode> toVisit,
        Dictionary<SurgeryNode, SurgeryEdge> pendingEdges,
        SurgeryGraph sourceGraph,
        SurgeryGraph mergedGraph,
        Dictionary<SurgeryNode, SurgeryNode> nodeMap,
        HashSet<SurgeryNode> explored)
    {
        while (toVisit.TryDequeue(out var currentSource))
        {
            if (!nodeMap.TryGetValue(currentSource, out var newNode))
            {
                newNode = new SurgeryNode
                {
                    Special = new(currentSource.Special)
                };
                var newId = mergedGraph.GetNextId();
                mergedGraph.Nodes[newId] = newNode;
                nodeMap[currentSource] = newNode;
            }

            if (!explored.Add(currentSource))
                continue;

            foreach (var edge in currentSource.Edges)
            {
                var newEdge = new SurgeryEdge
                {
                    Requirement = edge.Requirement
                };
                newNode.Edges.Add(newEdge);

                if (!sourceGraph.TryFindNode(edge.Connection, out var nextSource))
                {
                    newEdge.Connection = null;
                    continue;
                }

                if (pendingEdges.TryAdd(nextSource, newEdge))
                    toVisit.Enqueue(nextSource);

            }
        }
    }

    private static void ResolvePendingConnections(
        Dictionary<SurgeryNode, SurgeryEdge> pendingEdges,
        Dictionary<SurgeryNode, SurgeryNode> nodeMap,
        SurgeryGraph mergedGraph)
    {
        foreach (var (sourceNode, edge) in pendingEdges)
        {
            if (!nodeMap.TryGetValue(sourceNode, out var mapped))
                continue;

            if (mergedGraph.TryFindNodeId(mapped, out var id))
                edge.Connection = id;
        }
    }
}
