using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
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

            MergeGraphs(prototype, mergedGraph);
        }

        Graphs.Add(prototypeIds, mergedGraph);
        return mergedGraph;
    }

    private void MergeGraphs(SurgeryGraph targetGraph, SurgeryGraph mergedGraph)
    {
        // we go from start to end as nodes diverge more the further along the graph they go
        if (!targetGraph.TryGetStaringNode(out var targetStartingNode))
            return;

        if (!mergedGraph.TryGetStaringNode(out var mergedStartingNode))
        {
            mergedGraph.Nodes.Add(targetStartingNode);
            mergedStartingNode = targetStartingNode;
        }

        Stack<SurgeryNode> targetNodes = new();
        Stack<SurgeryNode> mergedNodes = new();

        targetNodes.Push(targetStartingNode);
        mergedNodes.Push(mergedStartingNode);

        // keep track of nodes weve seen before due to circular nodes
        HashSet<SurgeryNode> foundNodes = new();

        while (true)
        {
            if (!targetNodes.TryPop(out var targetNode) || !mergedNodes.TryPop(out var mergedNode))
                break;

            if (!foundNodes.Add(targetNode))
                break;

            foreach (var targetEdge in targetNode.Edges)
            {
                foreach (var mergedEdge in mergedNode.Edges)
                {
                    if (mergedEdge.Requirement.RequirementsMatch(targetEdge.Requirement, out var newRequirement))
                    {
                        if (targetEdge.Connection != null)
                            targetNodes.Push(targetEdge.Connection);

                        if (mergedEdge.Connection != null)
                            mergedNodes.Push(mergedEdge.Connection);

                        mergedEdge.Requirement = newRequirement;
                        continue;
                    }

                    //diverting edge
                    if (TryFindMatchingNode(targetEdge.Connection, mergedGraph.Nodes, out var matchingNode))
                    {
                        if (targetEdge.Connection != null)
                            targetNodes.Push(targetEdge.Connection);

                        mergedNodes.Push(matchingNode);
                    }
                    else
                    {
                        SurgeryNode newNode = new();
                        newNode.Edges.Add(targetEdge);
                        newNode.Special.Union(targetNode.Special);
                        newNode.Connections.Union(targetNode.Connections);
                        mergedGraph.Nodes.Add(newNode);

                        ExtendGraph(targetEdge.Connection, newNode, mergedGraph);
                    }
                }
            }
        }
    }

    private void ExtendGraph(SurgeryNode? currentTargetNode, SurgeryNode currentMergedNode, SurgeryGraph mergedGraph)
    {
        if (currentTargetNode == null)
            return;

        // avoid circular references
        HashSet<SurgeryNode> visitedNodes = new();

        Stack<(SurgeryNode TargetNode, SurgeryNode MergedNode)> stack = new();
        stack.Push((currentTargetNode, currentMergedNode));

        while (stack.TryPop(out var pair))
        {
            var targetNode = pair.TargetNode;
            var mergedNode = pair.MergedNode;

            if (!visitedNodes.Add(targetNode))
                continue;

            foreach (var edge in targetNode.Edges)
            {
                if (TryFindMatchingNode(edge.Connection, mergedGraph.Nodes, out var matchingNode))
                {
                    mergedNode.Edges.Add(new SurgeryEdge
                    {
                        Connection = matchingNode,
                        Requirement = edge.Requirement
                    });
                }
                else if (edge.Connection != null)
                {
                    SurgeryNode newMergedNode = new()
                    {
                        Special = MergeSpecialArrays(mergedNode.Special, edge.Connection.Special),
                        Connections = new HashSet<SurgeryEdge>(edge.Connection.Connections)
                    };

                    newMergedNode.Edges.Add(new SurgeryEdge
                    {
                        Connection = newMergedNode,
                        Requirement = edge.Requirement
                    });

                    mergedGraph.Nodes.Add(newMergedNode);

                    stack.Push((edge.Connection, newMergedNode));
                }
            }
        }
    }

    private static SurgerySpecial[] MergeSpecialArrays(SurgerySpecial[] array1, SurgerySpecial[] array2)
    {
        return array1.Concat(array2).Distinct().ToArray();
    }

    private bool TryFindMatchingNode(SurgeryNode? targetNode, List<SurgeryNode> nodes, [NotNullWhen(true)] out SurgeryNode? matchingNode)
    {
        matchingNode = null;

        if (targetNode == null)
            return false;

        foreach (var node in nodes)
        {
            var targetFailed = false;

            foreach (var targetEdge in targetNode.Edges)
            {
                var edgeFailed = false;

                foreach (var nodeEdge in node.Edges)
                {
                    if (!nodeEdge.Requirement.RequirementsMatch(targetEdge.Requirement, out _))
                    {
                        edgeFailed = true;
                        break;
                    }
                }

                if (edgeFailed)
                {
                    targetFailed = true;
                    break;
                }
            }

            if (targetFailed)
                return matchingNode != null;

            matchingNode = node;
        }

        return false;
    }
}
