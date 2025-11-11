using Content.Shared.Surgery;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Content.Client.Surgery.UI;

public sealed partial class SurgeryGraphControl
{
    private bool IsSelfLoop(SurgeryEdge edge, SurgeryGraph graph, [NotNullWhen(false)] out SurgeryNode? connectedNode, out Vector2? nodePos)
    {
        if (edge.Connection == null)
        {
            connectedNode = null;
            nodePos = null;
            return true;
        }
        else
        {
            connectedNode = graph.Nodes[edge.Connection.Value];

            if (_nodePositions == null)
            {
                nodePos = null;
            }
            else
            {
                _nodePositions.TryGetValue(connectedNode, out var pos);
                nodePos = pos;
            }

            return false;
        }
    }

    private static bool PathIntersectsAnything(Vector2[] path, Dictionary<SurgeryNode, Vector2> nodes, List<Vector2[]> edges)
    {
        foreach (var pos in nodes.Values)
        {
            for (var i = 0; i < path.Length - 1; i++)
            {
                var a = path[i];
                var b = path[i + 1];

                if (MathF.Abs(a.X - b.X) < 0.5f || MathF.Abs(a.Y - b.Y) < 0.5f)
                    continue;

                if (PointLineDistance(a, b, pos) < NodeRadius + EdgeClearance)
                    return true;
            }
        }

        foreach (var edge in edges)
        {
            for (var i = 0; i < path.Length - 1; i++)
            {
                if (LinesIntersect(path[i], path[i + 1], edge[0], edge[1]))
                    return true;
            }
        }

        return false;
    }

    private static Vector2 GetMidpointOfPolyline(Vector2[] points)
    {
        var totalLength = 0f;
        List<float> segmentLengths = [];

        for (var i = 0; i < points.Length - 1; i++)
        {
            var len = (points[i + 1] - points[i]).Length();
            segmentLengths.Add(len);
            totalLength += len;
        }

        var halfLength = totalLength / 2f;
        var accumulated = 0f;

        for (var i = 0; i < segmentLengths.Count; i++)
        {
            if (accumulated + segmentLengths[i] >= halfLength)
            {
                var segmentStart = points[i];
                var segmentEnd = points[i + 1];
                var remain = halfLength - accumulated;
                var t = remain / segmentLengths[i];
                return segmentStart + (segmentEnd - segmentStart) * t;
            }
            accumulated += segmentLengths[i];
        }

        // fallback
        return points[0];
    }

    private static Vector2 CalculateCubicBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        var u = 1 - t;
        var uu = u * u;
        var uuu = uu * u;
        var tt = t * t;
        var ttt = tt * t;

        var point = uuu * p0;
        point += 3 * uu * t * p1;
        point += 3 * u * tt * p2;
        point += ttt * p3;

        return point;
    }

    private static Vector2 CalculateCubicBezierTangent(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        var u = 1 - t;
        var uu = u * u;
        var tt = t * t;

        var point = 3 * uu * (p1 - p0);
        point += 6 * u * t * (p2 - p1);
        point += 3 * tt * (p3 - p2);

        return point;
    }

    private static void GetBackwardBezierPoints(
        Vector2 startPos,
        Vector2 endPos,
        out Vector2 adjustedStart,
        out Vector2 control1,
        out Vector2 control2,
        out Vector2 adjustedEnd)
    {
        control1 = new Vector2(startPos.X - BackwardEdgeControlOffsetX, startPos.Y - BackwardEdgeCurveHeight);
        control2 = new Vector2(endPos.X - BackwardEdgeControlOffsetX, endPos.Y - BackwardEdgeCurveHeight);

        var tangentStart = CalculateCubicBezierTangent(0f, startPos, control1, control2, endPos).Normalized();
        var tangentEnd = CalculateCubicBezierTangent(1f, startPos, control1, control2, endPos).Normalized();

        // offset start and end further along their tangents by clearance to ensure no overlap
        adjustedStart = startPos + tangentStart * (NodeRadius + EdgeClearance);
        adjustedEnd = endPos - tangentEnd * (NodeRadius + EdgeClearance);

        // recalculate control points relative to adjusted start/end, maintaining the arc shape
        control1 = new Vector2(adjustedStart.X - BackwardEdgeControlOffsetX, adjustedStart.Y - BackwardEdgeCurveHeight);
        control2 = new Vector2(adjustedEnd.X - BackwardEdgeControlOffsetX, adjustedEnd.Y - BackwardEdgeCurveHeight);
    }

    private SurgeryNode? GetNodeAtPosition(Vector2 position)
    {
        if (_nodePositions == null)
            return null;

        foreach (var (node, nodePos) in _nodePositions)
        {
            if (Vector2.Distance(position, nodePos) <= NodeRadius)
                return node;
        }

        return null;
    }

    private SurgeryEdge? GetEdgeAtPosition(Vector2 position)
    {
        if (_graph == null || _nodePositions == null || _layerMap == null)
            return null;

        foreach (var (node, nodePos) in _nodePositions)
        {
            foreach (var edge in node.Edges)
            {
                if (IsSelfLoop(edge, _graph, out var target, out var targetPos))
                {
                    target = node;
                    targetPos = nodePos;
                }

                if (targetPos == null)
                    continue;

                if (IsPointOnEdge(position, nodePos, targetPos.Value, node, target, _layerMap))
                    return edge;
            }
        }

        return null;
    }

    private static bool IsPointOnEdge(Vector2 point, Vector2 startPos, Vector2 endPos, SurgeryNode from, SurgeryNode to, Dictionary<SurgeryNode, int> layers)
    {
        var fromLayer = layers[from];
        var toLayer = layers[to];
        var isBackwards = toLayer < fromLayer;

        var direction = (endPos - startPos).Normalized();
        var start = startPos + direction * (NodeRadius + EdgeClearance);
        var end = endPos - direction * (NodeRadius + EdgeClearance);

        // self loops
        if (from == to)
        {
            var center = startPos + new Vector2(0, -SelfLoopYOffset);
            return Vector2.Distance(point, center) <= SelfLoopRadius + EdgeHoverDetectionWidth * CurvedEdgeHoverWidthMultiplier;
        }

        // bezier curves
        if (isBackwards)
        {
            GetBackwardBezierPoints(start, end, out start, out var control1, out var control2, out end);
            return IsPointNearBezier(point, start, control1, control2, end, EdgeHoverDetectionWidth * CurvedEdgeHoverWidthMultiplier);
        }

        // straight edges
        return PointLineDistance(start, end, point) <= EdgeHoverDetectionWidth;
    }

    private static bool IsPointNearBezier(Vector2 point, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float maxDistance)
    {
        if (!IsPointNearPolygon(point, [p0, p1, p2, p3], maxDistance * BezierEarlyRejectionMultiplier))
            return false;

        var prevPoint = p0;
        for (var i = 1; i <= BezierSegments; i++)
        {
            var t = i / (float)BezierSegments;
            var curvePoint = CalculateCubicBezierPoint(t, p0, p1, p2, p3);

            if (PointLineDistance(prevPoint, curvePoint, point) <= maxDistance)
                return true;

            prevPoint = curvePoint;
        }
        return false;
    }

    private static bool IsPointNearPolygon(Vector2 point, Vector2[] polygon, float maxDistance)
    {
        for (var i = 0; i < polygon.Length - 1; i++)
        {
            if (PointLineDistance(polygon[i], polygon[i + 1], point) <= maxDistance)
                return true;
        }
        return false;
    }

    private static float PointLineDistance(Vector2 a, Vector2 b, Vector2 p)
    {
        var l2 = (b - a).LengthSquared();
        if (l2 == 0)
            return Vector2.Distance(p, a);

        var t = MathF.Max(0, MathF.Min(1, Vector2.Dot(p - a, b - a) / l2));
        var projection = a + t * (b - a);
        return Vector2.Distance(p, projection);
    }

    private static bool LinesIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        var o1 = Orientation(a1, a2, b1);
        var o2 = Orientation(a1, a2, b2);
        var o3 = Orientation(b1, b2, a1);
        var o4 = Orientation(b1, b2, a2);

        if (o1 * o2 < 0 && o3 * o4 < 0)
            return true;

        return o1 == 0 && OnSegment(a1, a2, b1) ||
               o2 == 0 && OnSegment(a1, a2, b2) ||
               o3 == 0 && OnSegment(b1, b2, a1) ||
               o4 == 0 && OnSegment(b1, b2, a2);
    }

    private static float Orientation(Vector2 p, Vector2 q, Vector2 r)
    {
        return (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
    }

    private static bool OnSegment(Vector2 a, Vector2 b, Vector2 p)
    {
        return p.X <= Math.Max(a.X, b.X) && p.X >= Math.Min(a.X, b.X) &&
           p.Y <= Math.Max(a.Y, b.Y) && p.Y >= Math.Min(a.Y, b.Y);
    }
}
