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
                    targetGraph.TryFindNode(targetEdge.Connection, out var targetEdgeNode);

                    if (mergedEdge.Requirement.RequirementsMatch(targetEdge.Requirement, out var newRequirement))
                    {
                        if (targetEdgeNode != null)
                            targetNodes.Push(targetEdgeNode);

                        if (mergedGraph.TryFindNode(mergedEdge.Connection, out var mergedEdgeNode))
                            mergedNodes.Push(mergedEdgeNode);

                        mergedEdge.Requirement = newRequirement;
                        continue;
                    }
                    else if (targetEdgeNode == null)
                    {
                        continue;
                    }

                    //diverting edge
                    if (TryFindMatchingNode(targetEdgeNode, mergedGraph.Nodes, out var matchingNode))
                    {
                        if (targetEdge.Connection != null)
                            targetNodes.Push(targetEdgeNode);

                        mergedNodes.Push(matchingNode);
                    }
                    else
                    {
                        SurgeryNode newNode = new();
                        newNode.Edges.Add(targetEdge);
                        newNode.Special.Union(targetNode.Special);
                        mergedGraph.Nodes.Add(newNode);

                        ExtendGraph(targetEdgeNode, newNode, targetGraph, mergedGraph);
                    }
                }
            }
        }
    }

    private void ExtendGraph(SurgeryNode? currentTargetNode, SurgeryNode currentMergedNode, SurgeryGraph targetGraph, SurgeryGraph mergedGraph)
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
                if (!targetGraph.TryFindNode(edge.Connection, out var edgeConnection))
                    continue;

                if (TryFindMatchingNode(edgeConnection, mergedGraph.Nodes, out var matchingNode))
                {
                    mergedNode.Edges.Add(new SurgeryEdge
                    {
                        Connection = matchingNode.GetHashCode(),
                        Requirement = edge.Requirement
                    });
                }
                else if (edge.Connection != null)
                {
                    SurgeryNode newMergedNode = new()
                    {
                        Special = MergeSpecialArrays(mergedNode.Special, edgeConnection.Special),
                    };

                    newMergedNode.Edges.Add(new SurgeryEdge
                    {
                        Connection = newMergedNode.GetHashCode(),
                        Requirement = edge.Requirement
                    });

                    mergedGraph.Nodes.Add(newMergedNode);

                    stack.Push((edgeConnection, newMergedNode));
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
