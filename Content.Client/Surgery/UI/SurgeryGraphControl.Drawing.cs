using Content.Shared.Body.Part;
using Content.Shared.Surgery;
using Robust.Client.Graphics;
using System.Numerics;
using System.Linq;

namespace Content.Client.Surgery.UI;

public sealed partial class SurgeryGraphControl
{
    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_graph == null || _nodePositions == null || _layerMap == null)
            return;

        List<Vector2[]> drawnEdges = [];
        HashSet<(SurgeryNode, SurgeryNode?)> drawnPairs = [];

        var previous = handle.GetTransform();
        handle.SetTransform(Transform * previous);

        foreach (var (node, pos) in _nodePositions)
        {
            var nodeColor = node == CurrentNode
                ? CurrentNodeColor
                : HighlightedNodes.Contains(node)
                    ? NodeHighlightColor
                    : NodeColor;

            DrawNode(handle, pos, nodeColor);

            if (_hoveredNode == node)
                DrawNode(handle, pos, NodeHoverColor, false);

            foreach (var edge in node.Edges)
            {
                if (IsSelfLoop(edge, _graph, out var target, out var targetPos))
                {
                    var isHighlighted = HighlightedNodes.Contains(node);
                    var edgeColor = _hoveredEdge == edge
                        ? EdgeHoverColor
                        : isHighlighted
                            ? EdgeHighlightColor
                            : EdgeColor;

                    DrawSelfLoop(handle, pos, edgeColor);
                }
                else
                {
                    if (targetPos == null)
                        continue;

                    if (drawnPairs.Contains((target, node)))
                        continue;

                    var key = (node, target);
                    if (drawnPairs.Contains(key))
                        continue;

                    drawnPairs.Add(key);

                    var isHighlighted = HighlightedNodes.Contains(node) && (target == null || HighlightedNodes.Contains(target));
                    var edgeColor = _hoveredEdge == edge
                        ? EdgeHoverColor
                        : isHighlighted
                            ? EdgeHighlightColor
                            : EdgeColor;

                    DrawEdge(handle, pos, targetPos.Value, node, target!, edgeColor, _layerMap, _nodePositions, drawnEdges);
                }
            }
        }

        handle.SetTransform(previous);
    }

    private static void DrawNode(DrawingHandleScreen handle, Vector2 pos, Color color, bool filled = true)
    {
        handle.DrawCircle(pos, NodeRadius, color, filled: filled);
    }

    private static void DrawSelfLoop(DrawingHandleScreen handle, Vector2 pos, Color color)
    {
        var start = pos + new Vector2(-SelfLoopRadius, -SelfLoopYOffset);
        var end = pos + new Vector2(SelfLoopRadius, -SelfLoopYOffset);
        var control1 = pos + new Vector2(-SelfLoopRadius, -SelfLoopRadius - SelfLoopYOffset);
        var control2 = pos + new Vector2(SelfLoopRadius, -SelfLoopRadius - SelfLoopYOffset);

        DrawBezier(handle, start, control1, control2, end, color);

        var arrowBase = CalculateCubicBezierPoint(BezierArrowOffsetT, start, control1, control2, end);
        var arrowTip = CalculateCubicBezierPoint(BezierArrowTipT, start, control1, control2, end);
        DrawArrowHead(handle, arrowBase, arrowTip, color);
    }

    private static void DrawEdge(
        DrawingHandleScreen handle,
        Vector2 startPos,
        Vector2 endPos,
        SurgeryNode from,
        SurgeryNode to,
        Color color,
        Dictionary<SurgeryNode, int> layers,
        Dictionary<SurgeryNode, Vector2> nodePositions,
        List<Vector2[]> existingEdges)
    {
        var direction = (endPos - startPos).Normalized();
        var start = startPos + direction * (NodeRadius + EdgeClearance);
        var end = endPos - direction * (NodeRadius + EdgeClearance);

        var fromLayer = layers[from];
        var toLayer = layers[to];
        var layerDiff = Math.Abs(toLayer - fromLayer);

        var isBackwards = toLayer < fromLayer;
        if (isBackwards)
        {
            var control1 = new Vector2(start.X - BackwardEdgeControlOffsetX, start.Y - BackwardEdgeCurveHeight);
            var control2 = new Vector2(end.X - BackwardEdgeControlOffsetX, end.Y - BackwardEdgeCurveHeight);

            var tangentStart = CalculateCubicBezierTangent(0f, start, control1, control2, end).Normalized();
            var tangentEnd = CalculateCubicBezierTangent(1f, start, control1, control2, end).Normalized();

            // offset start and end further along their tangents by clearance to ensure no overlap
            var adjustedStart = start + tangentStart * (NodeRadius + EdgeClearance);
            var adjustedEnd = end - tangentEnd * (NodeRadius + EdgeClearance);

            // recalculate control points relative to adjusted start/end, maintaining the arc shape
            control1 = new Vector2(adjustedStart.X - BackwardEdgeControlOffsetX, adjustedStart.Y - BackwardEdgeCurveHeight);
            control2 = new Vector2(adjustedEnd.X - BackwardEdgeControlOffsetX, adjustedEnd.Y - BackwardEdgeCurveHeight);

            DrawBezier(handle, adjustedStart, control1, control2, adjustedEnd, color);

            var arrowBase = CalculateCubicBezierPoint(BezierArrowOffsetT, adjustedStart, control1, control2, adjustedEnd);
            var arrowTip = CalculateCubicBezierPoint(BezierArrowTipT, adjustedStart, control1, control2, adjustedEnd);
            DrawArrowHead(handle, arrowBase, arrowTip, color);
            return;
        }

        if (!PathIntersectsAnything([start, end], nodePositions, existingEdges))
        {
            DrawLineSegment(handle, start, end, existingEdges, color, isFinal: true);
            return;
        }

        // multi layer edge routing
        if (layerDiff > 1)
        {
            List<Vector2> points = [];
            points.Add(start);
            for (var i = 1; i < layerDiff; i++)
            {
                var y = start.Y + (end.Y - start.Y) * i / layerDiff;
                points.Add(new Vector2(start.X, y));
            }
            points.Add(end);

            if (!PathIntersectsAnything(points.ToArray(), nodePositions, existingEdges))
            {
                for (var i = 0; i < points.Count - 1; i++)
                    DrawLineSegment(handle, points[i], points[i + 1], existingEdges, color, i == points.Count - 2);

                return;
            }
        }

        var match = from.Edges.FirstOrDefault(e => e.Connection?.Equals(to.Id) == true);
        if (match == null)
        {
            DrawLineSegment(handle, start, end, existingEdges, color, isFinal: true);
            return;
        }

        // branch aware elbow fallback
        var branchIndex = from.Edges.IndexOf(match);
        var totalBranches = from.Edges.Count;
        var dx = (branchIndex - totalBranches / 2f) * BranchSpacing;

        var mid1 = new Vector2(start.X + dx, start.Y);
        var mid2 = new Vector2(start.X + dx, end.Y);
        Vector2[] elbowPath = [start, mid1, mid2, end];

        if (!PathIntersectsAnything(elbowPath, nodePositions, existingEdges))
        {
            DrawLineSegment(handle, start, mid1, existingEdges, color);
            DrawLineSegment(handle, mid1, mid2, existingEdges, color);
            DrawLineSegment(handle, mid2, end, existingEdges, color, isFinal: true);
            return;
        }

        // raw direct
        DrawLineSegment(handle, start, end, existingEdges, color, isFinal: true);
    }

    public static void DrawBezier(
        DrawingHandleScreen handle,
        Vector2 start,
        Vector2 control1,
        Vector2 control2,
        Vector2 end,
        Color color)
    {
        var points = new Vector2[BezierSegments + 1];

        for (var i = 0; i <= BezierSegments; i++)
        {
            var t = i / (float)BezierSegments;
            points[i] = CalculateCubicBezierPoint(t, start, control1, control2, end);
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, points, color);
    }

    private static void DrawLineSegment(DrawingHandleScreen handle, Vector2 start, Vector2 end, List<Vector2[]> drawnEdges, Color color, bool isFinal = false)
    {
        handle.DrawLine(start, end, color);
        drawnEdges.Add([start, end]);

        if (isFinal)
            DrawArrowHead(handle, start, end, color);
    }

    private static void DrawArrowHead(DrawingHandleScreen handle, Vector2 start, Vector2 end, Color color)
    {
        var direction = (end - start).Normalized();
        var perpendicular = new Vector2(-direction.Y, direction.X);

        var basePoint = end - direction * NodeRadius;
        var arrow1 = basePoint + perpendicular * EdgeArrowSize - direction * EdgeArrowSize;
        var arrow2 = basePoint - perpendicular * EdgeArrowSize - direction * EdgeArrowSize;

        handle.DrawLine(end, arrow1, color);
        handle.DrawLine(end, arrow2, color);
    }

    private void DrawEdgeIcon(DrawingHandleScreen handle, SurgeryEdge edge, EntityUid? body, EntityUid? limb, BodyPart part)
    {
        if (!_edgeIcons.TryGetValue(edge, out var icon))
        {
            icon = edge.Requirement.GetIcon(body, limb, part);
            _edgeIcons[edge] = icon;
        }

        if (icon == null)
            return;
    }
}
