using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Surgery.Systems;

public sealed partial class SurgerySystem
{
    // calculating the graph is expensive and reused multiple times so cache it
    private Dictionary<List<ProtoId<SurgeryPrototype>>, SurgeryGraph> Graphs = new();

    public SurgeryGraph MergeGraphs(List<ProtoId<SurgeryPrototype>> prototypeIds)
    {
        if (Graphs.ContainsKey(prototypeIds))
            return Graphs[prototypeIds];

        SurgeryGraph mergedGraph = new();

        foreach (var prototypeId in prototypeIds)
        {
            if (!_prototype.TryIndex<SurgeryPrototype>(prototypeId, out var prototype))
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

        var nodeMap = new Dictionary<SurgeryNode, SurgeryNode>();

        if (!mergedGraph.TryGetStaringNode(out var mergedStartingNode))
        {
            mergedStartingNode = CloneNode(targetStartingNode);
            mergedGraph.Nodes.Add(mergedGraph.GetNextId(), mergedStartingNode);
            mergedGraph.StartingNode = mergedGraph.Nodes.First().Key;
            nodeMap[targetStartingNode] = mergedStartingNode;
        }
        else
        {
            if (AreNodesEquivalent(targetStartingNode, mergedStartingNode))
            {
                MergeNodeContents(mergedStartingNode, targetStartingNode);
                nodeMap[targetStartingNode] = mergedStartingNode;
            }
            else
            {
                var newStart = CloneNode(targetStartingNode);
                mergedGraph.Nodes.Add(mergedGraph.GetNextId(), newStart);
                nodeMap[targetStartingNode] = newStart;
            }
        }

        var queue = new Queue<SurgeryNode>();
        queue.Enqueue(targetStartingNode);
        var visited = new HashSet<SurgeryNode>();

        while (queue.Count > 0)
        {
            var targetNode = queue.Dequeue();
            if (!visited.Add(targetNode))
                continue;

            if (!nodeMap.TryGetValue(targetNode, out var mergedNode))
            {
                mergedNode = CloneNode(targetNode);
                mergedGraph.Nodes.Add(mergedGraph.GetNextId(), mergedNode);
                nodeMap[targetNode] = mergedNode;
            }

            foreach (var targetEdge in targetNode.Edges)
            {
                if (!targetGraph.TryFindNode(targetEdge.Connection, out var targetEdgeNode))
                    continue;

                var mergedEdgeNode = FindOrCreateEquivalentNode(targetEdgeNode, mergedGraph, nodeMap);

                if (!mergedNode.Edges.Any(e =>
                    e.Requirement.RequirementsMatch(targetEdge.Requirement, out _) &&
                    e.Connection == mergedGraph.Nodes.First(kvp => kvp.Value == mergedEdgeNode).Key))
                {
                    var newEdge = new SurgeryEdge
                    {
                        Requirement = targetEdge.Requirement,
                        Connection = mergedGraph.Nodes.First(kvp => kvp.Value == mergedEdgeNode).Key
                    };
                    mergedNode.Edges.Add(newEdge);
                }

                queue.Enqueue(targetEdgeNode);
            }
        }
    }

    private SurgeryNode FindOrCreateEquivalentNode(SurgeryNode targetNode, SurgeryGraph mergedGraph, Dictionary<SurgeryNode, SurgeryNode> nodeMap)
    {
        if (nodeMap.TryGetValue(targetNode, out var existing))
            return existing;

        // look for equivalent node in merged graph
        foreach (var mergedNode in mergedGraph.Nodes.Values)
        {
            if (AreNodesEquivalent(targetNode, mergedNode))
            {
                MergeNodeContents(mergedNode, targetNode);
                nodeMap[targetNode] = mergedNode;
                return mergedNode;
            }
        }

        // no equivalent found
        // create new node
        var newNode = CloneNode(targetNode);
        mergedGraph.Nodes.Add(mergedGraph.GetNextId(), newNode);
        nodeMap[targetNode] = newNode;
        return newNode;
    }

    private bool AreNodesEquivalent(SurgeryNode a, SurgeryNode b)
    {
        if (a.Edges.Count != b.Edges.Count) return false;

        for (int i = 0; i < a.Edges.Count; i++)
        {
            var edgeA = a.Edges[i];
            var edgeB = b.Edges[i];

            if (!edgeA.Requirement.RequirementsMatch(edgeB.Requirement, out _))
                return false;
        }
        return true;
    }

    private void MergeNodeContents(SurgeryNode mergedNode, SurgeryNode newContent)
    {
        var mergedSpecials = new List<SurgerySpecial>(mergedNode.Special);
        foreach (var special in newContent.Special)
        {
            if (!mergedSpecials.Any(s => s.GetType() == special.GetType() && s.Equals(special)))
                mergedSpecials.Add(special);
        }
        mergedNode.Special = mergedSpecials.ToArray();

        foreach (var newEdge in newContent.Edges)
        {
            if (!mergedNode.Edges.Any(e => e.Requirement.RequirementsMatch(newEdge.Requirement, out _)))
                mergedNode.Edges.Add(new SurgeryEdge
                {
                    Requirement = newEdge.Requirement,
                    Connection = newEdge.Connection
                });
        }
    }

    private SurgeryNode CloneNode(SurgeryNode source)
    {
        return new SurgeryNode
        {
            Special = source.Special.ToArray(),
            Edges = source.Edges.Select(e => new SurgeryEdge
            {
                Requirement = e.Requirement,
                Connection = e.Connection
            }).ToList()
        };
    }
}
