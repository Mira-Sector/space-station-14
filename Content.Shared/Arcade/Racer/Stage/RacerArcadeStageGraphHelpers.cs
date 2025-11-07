using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Arcade.Racer.Stage;

public static class RacerArcadeStageGraphHelpers
{
    public static bool TryGetNextNode(this RacerArcadeStageGraph graph, IRacerArcadeStageEdge edge, [NotNullWhen(true)] out RacerArcadeStageNode? node)
    {
        if (edge is not RacerArcadeStageEdgeNode edgeNode)
        {
            node = null;
            return false;
        }

        return graph.TryGetNode(edgeNode.ConnectionId, out node);
    }

    public static bool TryGetNewGraph(this IRacerArcadeStageEdge edge, IPrototypeManager prototypeManager, [NotNullWhen(true)] out RacerArcadeStageGraph? graph)
    {
        if (edge is not RacerArcadeStageEdgeStage edgeStage)
        {
            graph = null;
            return false;
        }

        var stage = prototypeManager.Index(edgeStage.StageId);
        graph = stage.Graph;
        return true;
    }

    public static bool TryTraverseEdge(
        this RacerArcadeStageGraph graph,
        IRacerArcadeStageEdge edge,
        IPrototypeManager prototypeManager,
        out RacerArcadeStageNode? nextNode,
        out RacerArcadeStageGraph? nextGraph)
    {
        if (graph.TryGetNextNode(edge, out var node))
        {
            nextNode = node;
            nextGraph = null;
            return true;
        }

        if (edge.TryGetNewGraph(prototypeManager, out var graph2) && graph2.TryGetStartingNode(out var starting))
        {
            nextNode = starting;
            nextGraph = graph2;
            return true;
        }

        nextGraph = null;
        nextNode = null;
        return false;
    }

    public static bool TryGetParentNode(this RacerArcadeStageGraph graph, IRacerArcadeStageEdge edge, [NotNullWhen(true)] out RacerArcadeStageNode? node)
    {
        foreach (var x in graph.Nodes.Values)
        {
            if (!x.Connections.Contains(edge))
                continue;

            node = x;
            return true;
        }

        node = null;
        return false;
    }
}
