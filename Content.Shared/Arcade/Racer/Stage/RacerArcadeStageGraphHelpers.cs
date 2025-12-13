using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Arcade.Racer.Stage;

public static class RacerArcadeStageGraphHelpers
{
    private const int BezierSamples = 16;

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

    public static IEnumerable<(IRacerArcadeStageEdge Edge, RacerArcadeStageNode Parent)> GetConnections(this RacerArcadeStageGraph graph)
    {
        foreach (var node in graph.Nodes.Values)
        {
            foreach (var edge in node.Connections)
                yield return (edge, node);
        }
    }

    public static Vector3[] SampleBezier(Vector3[] points, int resolution)
    {
        var result = new Vector3[resolution + 1];
        for (var i = 0; i <= resolution; i++)
        {
            var t = i / (float)resolution;
            result[i] = EvaluateBezier(points, t);
        }
        return result;
    }

    public static Vector3 EvaluateBezier(Vector3[] pts, float t)
    {
        var temp = new Vector3[pts.Length];
        Array.Copy(pts, temp, pts.Length);
        for (var k = pts.Length - 1; k > 0; k--)
        {
            for (var i = 0; i < k; i++)
                temp[i] = Vector3.Lerp(temp[i], temp[i + 1], t);
        }
        return temp[0];
    }

    public static Vector3[] GetWorldSpaceEdgePoints(this IRacerArcadeStageRenderableEdge edge, Vector3 sourceNode, Vector3 nextNode)
    {
        var points = new Vector3[edge.ControlPoints.Length + 2];
        points[0] = sourceNode;

        if (edge.ControlPoints.Any())
        {
            for (var i = 0; i < edge.ControlPoints.Length; i++)
                points[i + 1] = edge.ControlPoints[i] + sourceNode;
        }
        points[^1] = nextNode;
        return points;
    }

    public static Quaternion GetEdgeDirection(Vector3 sourceNode, Vector3 nextNode)
    {
        var forward = nextNode - sourceNode;

        forward = Vector3.Normalize(forward);
        var up = Vector3.UnitZ;
        return Quaternion.LookRotation(ref forward, ref up);
    }

    public static Quaternion GetDirectionAtPosition(this RacerArcadeStageGraph graph, Vector3 position)
    {
        var minDistSqr = float.MaxValue;
        Vector3? closestTangent = null;

        foreach (var (edge, parent) in graph.GetConnections())
        {
            if (edge is not IRacerArcadeStageRenderableEdge renderableEdge)
                continue;

            if (!graph.TryGetNextNode(edge, out var nextNode))
                continue;

            var points = renderableEdge.GetWorldSpaceEdgePoints(parent.Position, nextNode.Position);
            var samples = SampleBezier(points, BezierSamples);
            for (var i = 1; i < samples.Length; i++)
            {
                var start = samples[i - 1];
                var end = samples[i];
                var segment = end - start;

                var t = Math.Clamp(Vector3.Dot(position - start, segment) / segment.LengthSquared, 0f, 1f);
                var proj = start + t * segment;
                var distSqr = (position - proj).LengthSquared;

                if (distSqr > minDistSqr)
                    continue;

                minDistSqr = distSqr;
                closestTangent = segment;
            }
        }

        if (closestTangent is not { } tangent)
            return Quaternion.Identity;

        var forward = Vector3.Normalize(tangent);
        var up = Vector3.UnitZ;
        return Quaternion.LookRotation(ref forward, ref up);
    }
}
