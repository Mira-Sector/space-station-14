using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Systems;

public abstract partial class SharedSurgerySystem
{
    // calculating the graph is expensive and reused multiple times so cache it
    private Dictionary<List<ProtoId<SurgeryPrototype>>, SurgeryGraph> Graphs = new();

    public SurgeryGraph MergeGraphs(List<ProtoId<SurgeryPrototype>> prototypeIds)
    {
        foreach (var (graphProtoIds, graph) in Graphs)
        {
            if (prototypeIds.Count != graphProtoIds.Count)
                continue;

            var failed = false;

            for (var i = 0; i < prototypeIds.Count; i++)
            {
                if (prototypeIds[i] != graphProtoIds[i])
                {
                    failed = true;
                    break;
                }
            }

            if (!failed)
                return graph;
        }

        SurgeryGraph mergedGraph = new();

        foreach (var prototypeId in prototypeIds)
        {
            if (!_prototype.TryIndex(prototypeId, out var prototype))
                continue;

            MergeGraphs(prototype, mergedGraph);
        }

        Graphs.Add(prototypeIds, mergedGraph);
        return mergedGraph;
    }

    private void MergeGraphs(SurgeryGraph targetGraph, SurgeryGraph mergedGraph)
    {
        if (!targetGraph.TryGetStaringNode(out var targetStartingNode))
            return;

        if (!mergedGraph.TryGetStaringNode(out var mergedStartingNode))
        {
            SetGraph(targetGraph, mergedGraph, targetStartingNode);
            return;
        }

        HashSet<SurgeryNode> exploredNodes = new();

        Queue<SurgeryNode> targetQueue = new();
        targetQueue.Enqueue(targetStartingNode);

        Dictionary<SurgeryNode, SurgeryNode> nodeMap = new();
        nodeMap.Add(targetStartingNode, mergedStartingNode);

        while (targetQueue.TryDequeue(out var targetNode))
        {
            // node already explored
            if (!exploredNodes.Add(targetNode))
                continue;

            if (!nodeMap.TryGetValue(targetNode, out var mergedNode))
                continue;

            GetMatchingEdges(targetNode, mergedNode, out var matchingEdges, out var missingEdges);

            foreach (var (targetEdge, mergedEdge) in matchingEdges)
            {
                if (targetGraph.TryFindNode(targetEdge.Connection, out var targetConnection))
                {
                    if (mergedGraph.TryFindNode(mergedEdge.Connection, out var mergedConnection))
                    {
                        targetQueue.Enqueue(targetConnection);
                        nodeMap.Add(targetConnection, mergedConnection);
                    }
                    else
                    {
                        ContinueGraph(targetEdge, targetNode, mergedNode, targetGraph, mergedGraph);
                    }
                }
            }

            foreach (var targetEdge in missingEdges)
            {
                ContinueGraph(targetEdge, targetNode, mergedNode, targetGraph, mergedGraph);
            }
        }
    }

    private void SetGraph(SurgeryGraph targetGraph, SurgeryGraph mergedGraph, SurgeryNode targetStartingNode)
    {
        var newId = mergedGraph.GetNextId();
        var newNode = new SurgeryNode();
        mergedGraph.Nodes.Add(newId, newNode);
        mergedGraph.StartingNode = newId;

        newNode.Special = new(targetStartingNode.Special);

        foreach (var edge in targetStartingNode.Edges)
            ContinueGraph(edge, targetStartingNode, newNode, targetGraph, mergedGraph);
    }

    private void GetMatchingEdges(SurgeryNode targetNode, SurgeryNode mergedNode, out Dictionary<SurgeryEdge, SurgeryEdge> matchingEdges, out HashSet<SurgeryEdge> missingEdges)
    {
        matchingEdges = new();
        missingEdges = new();

        if (targetNode.Special.Count != mergedNode.Special.Count)
            return;

        foreach (var targetSpecial in targetNode.Special)
        {
            if (!mergedNode.Special.Contains(targetSpecial))
                return;
        }

        foreach (var targetEdge in targetNode.Edges)
        {
            SurgeryEdge? matchingEdge = null;

            foreach (var mergedEdge in mergedNode.Edges)
            {
                if (!targetEdge.Requirement.RequirementsMatch(mergedEdge.Requirement, out var _))
                    continue;

                matchingEdge = mergedEdge;
                break;
            }

            if (matchingEdge != null)
            {
                matchingEdges.Add(targetEdge, matchingEdge);
            }
            else
            {
                missingEdges.Add(targetEdge);
            }
        }
    }

    private void ContinueGraph(SurgeryEdge sourceTargetEdge, SurgeryNode sourceTargetNode, SurgeryNode sourceMergedNode, SurgeryGraph targetGraph, SurgeryGraph mergedGraph)
    {
        if (!targetGraph.TryFindNode(sourceTargetEdge.Connection, out var sourceTargetConnection))
            return;

        Queue<SurgeryNode> targetNodes = new();
        targetNodes.Enqueue(sourceTargetConnection);

        // we need to keep track of what node sent us down this path
        // the edge connections need to be updated with new ids
        Dictionary<SurgeryNode, SurgeryEdge> connectionsNeedUpdate = new();
        Dictionary<SurgeryNode, SurgeryNode> nodeMap = new();

        var sourceMergedEdge = new SurgeryEdge();
        sourceMergedNode.Edges.Add(sourceMergedEdge);

        sourceMergedEdge.Requirement = sourceTargetEdge.Requirement;

        connectionsNeedUpdate.Add(sourceTargetConnection, sourceMergedEdge);
        nodeMap.Add(sourceTargetNode, sourceMergedNode);

        while (targetNodes.TryDequeue(out var targetNode))
        {
            if (nodeMap.ContainsKey(targetNode))
                continue;

            var id = mergedGraph.GetNextId();

            var newNode = new SurgeryNode()
            {
                Special = new(targetNode.Special)
            };

            nodeMap.Add(targetNode, newNode);

            foreach (var edge in targetNode.Edges)
            {
                var newEdge = new SurgeryEdge()
                {
                    Requirement = edge.Requirement
                };

                newNode.Edges.Add(newEdge);

                if (!targetGraph.TryFindNode(edge.Connection, out var targetConnection))
                {
                    newEdge.Connection = null;
                    continue;
                }

                if (!connectionsNeedUpdate.TryAdd(targetConnection, newEdge))
                {
                    // circular node
                    newEdge.Connection = null;
                    continue;
                }

                targetNodes.Enqueue(targetConnection);
            }

            mergedGraph.Nodes.Add(id, newNode);
        }

        foreach (var (node, edge) in connectionsNeedUpdate)
        {
            if (!mergedGraph.TryFindNodeId(nodeMap[node], out var id))
                continue;

            edge.Connection = id;
        }
    }
}
