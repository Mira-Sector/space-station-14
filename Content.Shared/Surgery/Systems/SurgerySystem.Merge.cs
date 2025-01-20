using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Surgery.Systems;

public sealed partial class SurgerySystem
{
    public SurgeryGraph MergeGraphs(List<ProtoId<SurgeryPrototype>> prototypeIds)
    {
        List<SurgeryGraph> graphs = new();

        foreach (var prototypeId in prototypeIds)
        {
            if (!_prototype.TryIndex<SurgeryPrototype>(prototypeId, out var prototype))
                continue;

            graphs.Add(prototype);
        }

        return MergeGraphs(graphs);
    }

    public SurgeryGraph MergeGraphs(List<SurgeryGraph> graphs)
    {
        SurgeryGraph mergedGraph = new();

        foreach (var graph in graphs)
        {
            if (!TryGetStaringNode(graph, out var start))
                continue;

            if (TryGetStaringNode(mergedGraph, out var mergedStart))
            {
                MergeNode(start, ref mergedStart);
            }
            else
            {
                mergedGraph.Nodes.Add(start);
            }

            foreach (var node in graph.Nodes)
            {
                if (TryGetNode(mergedGraph, node.ID, out var mergedNode))
                {
                    MergeNode(node, ref mergedNode);
                }
                else
                {
                    mergedGraph.Nodes.Add(node);
                }
            }
        }

        return mergedGraph;
    }

    public bool TryGetStaringNode(SurgeryGraph graph, [NotNullWhen(true)] out SurgeryNode? start)
    {
        return TryGetNode(graph, SurgeryGraph.StartingNode, out start);
    }

    public bool TryGetNode(SurgeryGraph graph, string nodeId, [NotNullWhen(true)] out SurgeryNode? foundNode)
    {
        foundNode = null;

        foreach (var node in graph.Nodes)
        {
            if (node.ID == nodeId)
            {
                foundNode = node;
                return true;
            }
        }

        return false;
    }

    public void MergeNode(SurgeryNode toMerge, ref SurgeryNode node)
    {
        node.Edges.UnionWith(toMerge.Edges);
        node.Special.Union(toMerge.Special);
    }
}
