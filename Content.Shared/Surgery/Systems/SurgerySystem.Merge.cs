using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Surgery.Systems;

public sealed partial class SurgerySystem
{
    // calculating the graph is expensive and reused multiple times so cache it
    Dictionary<List<ProtoId<SurgeryPrototype>>, SurgeryGraph> Graphs = new();

    public SurgeryGraph MergeGraphs(List<ProtoId<SurgeryPrototype>> prototypeIds)
    {
        if (Graphs.ContainsKey(prototypeIds))
            return Graphs[prototypeIds];

        SurgeryGraph mergedGraph = new();

        foreach (var prototypeId in prototypeIds)
        {
            if (!_prototype.TryIndex<SurgeryPrototype>(prototypeId, out var prototype))
                continue;

            MergeGraphs(mergedGraph, prototype);
        }

        Graphs.Add(prototypeIds, mergedGraph);
        return mergedGraph;
    }

    private void MergeGraphs(SurgeryGraph targetGraph, SurgeryPrototype prototype)
    {
        // maintain a mapping from nodes in the prototype to nodes in the target graph
        var nodeMapping = new Dictionary<SurgeryNode, SurgeryNode>();

        foreach (var prototypeNode in prototype.Nodes)
        {
            if (!NodeExistsInGraph(targetGraph, prototypeNode, out var existingNode))
            {
                var clonedNode = CloneNode(prototypeNode);
                targetGraph.Nodes.Add(clonedNode);
                nodeMapping[prototypeNode] = clonedNode;
            }
            else
            {
                if (existingNode != null && NodesAreEquivalent(existingNode, prototypeNode))
                {
                    nodeMapping[prototypeNode] = existingNode;
                }
                else
                {
                    var clonedNode = CloneNode(prototypeNode);
                    targetGraph.Nodes.Add(clonedNode);
                    nodeMapping[prototypeNode] = clonedNode;
                }
            }
        }

        foreach (var prototypeNode in prototype.Nodes)
        {
            var targetNode = nodeMapping[prototypeNode];
            foreach (var edge in prototypeNode.Edges)
            {
                // only add an edge if its not already present with identical requirements
                if (edge.Connection != null && nodeMapping.TryGetValue(edge.Connection, out var targetConnection))
                {
                    if (!EdgeExistsWithSameRequirements(targetNode, targetConnection, edge))
                    {
                        var clonedEdge = CloneEdge(edge);
                        clonedEdge.Connection = targetConnection;
                        targetNode.Edges.Add(clonedEdge);

                        targetConnection.Connections.Add(clonedEdge);
                    }
                }
            }
        }
    }

    private static bool NodeExistsInGraph(SurgeryGraph graph, SurgeryNode node, out SurgeryNode? existingNode)
    {
        foreach (var graphNode in graph.Nodes)
        {
            if (NodesAreEquivalent(graphNode, node))
            {
                existingNode = graphNode;
                return true;
            }
        }

        existingNode = null;
        return false;
    }

    private static SurgeryNode CloneNode(SurgeryNode node)
    {
        return new SurgeryNode
        {
            Special = node.Special.ToArray(),
            Edges = new HashSet<SurgeryEdge>()
        };
    }

    private static SurgeryEdge CloneEdge(SurgeryEdge edge)
    {
        return new SurgeryEdge
        {
            Requirements = edge.Requirements.ToArray()
        };
    }

    private static bool EdgeExistsWithSameRequirements(SurgeryNode sourceNode, SurgeryNode targetNode, SurgeryEdge newEdge)
    {
        foreach (var existingEdge in sourceNode.Edges)
        {
            if (existingEdge.Connection == targetNode && EdgeRequirementsAreEqual(existingEdge, newEdge))
            {
                return true;
            }
        }

        return false;
    }

    private static bool EdgeRequirementsAreEqual(SurgeryEdge edge1, SurgeryEdge edge2)
    {
        if (edge1.Requirements.Length != edge2.Requirements.Length)
            return false;

        for (int i = 0; i < edge1.Requirements.Length; i++)
        {
            if (!edge1.Requirements[i].Equals(edge2.Requirements[i]))
                return false;
        }

        return true;
    }

    private static bool NodesAreEquivalent(SurgeryNode node1, SurgeryNode node2)
    {
        if (node1.ID != node2.ID || node1.Special.Length != node2.Special.Length)
            return false;

        for (int i = 0; i < node1.Special.Length; i++)
        {
            if (!node1.Special[i].Equals(node2.Special[i]))
                return false;
        }

        // compare outgoing edges
        if (!EdgesAreEquivalent(node1.Edges, node2.Edges))
            return false;

        // compare incoming edges
        if (!EdgesAreEquivalent(node1.Connections, node2.Connections))
            return false;

        return true;
    }

    private static bool EdgesAreEquivalent(IEnumerable<SurgeryEdge> edges1, IEnumerable<SurgeryEdge> edges2)
    {
        var edgeList1 = edges1.ToList();
        var edgeList2 = edges2.ToList();

        if (edgeList1.Count != edgeList2.Count)
            return false;

        foreach (var edge1 in edgeList1)
        {
            if (!edgeList2.Any(edge2 =>
                EdgeRequirementsAreEqual(edge1, edge2) &&
                edge1.Connection != null &&
                edge2.Connection != null &&
                edge1.Connection == edge2.Connection))
            {
                return false;
            }
        }

        return true;
    }
}
