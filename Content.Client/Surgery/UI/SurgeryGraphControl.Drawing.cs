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

            if (_hoveredNode == node || _clickedNode == node)
            {
                DrawNode(handle, pos, NodeHoverColor);
                DrawNode(handle, pos, nodeColor, true);
            }
            else
            {
                DrawNode(handle, pos, nodeColor);
            }

            DrawNodeIcon(handle, node, pos);

            foreach (var edge in node.Edges)
            {
                if (IsSelfLoop(edge, _graph, out var target, out var targetPos))
                {
                    var isHighlighted = HighlightedNodes.Contains(node);
                    var isHovered = _hoveredEdge == edge || _clickedEdge == edge;
                    var edgeColor = isHovered
                        ? EdgeHoverColor
                        : isHighlighted
                            ? EdgeHighlightColor
                            : EdgeColor;

                    var midPoint = DrawSelfLoop(handle, pos, edgeColor);
                    DrawEdgeIcon(handle, edge, midPoint, edgeColor.WithAlpha(EdgeIconBackgroundAlpha));
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
                    var isHovered = _hoveredEdge == edge || _clickedEdge == edge;
                    var edgeColor = isHovered
                        ? EdgeHoverColor
                        : isHighlighted
                            ? EdgeHighlightColor
                            : EdgeColor;

                    var midPoint = DrawEdge(handle, pos, targetPos.Value, node, target!, edgeColor, _layerMap, _nodePositions, drawnEdges);
                    DrawEdgeIcon(handle, edge, midPoint, edgeColor.WithAlpha(EdgeIconBackgroundAlpha));
                }
            }
        }

        handle.SetTransform(previous);
    }

    private static void DrawNode(DrawingHandleScreen handle, Vector2 pos, Color color, bool inner = false)
    {
        var radius = inner ? NodeInnerRadius : NodeRadius;
        handle.DrawCircle(pos, radius, color);
    }

    private static Vector2 DrawSelfLoop(DrawingHandleScreen handle, Vector2 pos, Color color)
    {
        var start = pos + new Vector2(-SelfLoopRadius, -SelfLoopYOffset);
        var end = pos + new Vector2(SelfLoopRadius, -SelfLoopYOffset);
        var control1 = pos + new Vector2(-SelfLoopRadius, -SelfLoopRadius - SelfLoopYOffset);
        var control2 = pos + new Vector2(SelfLoopRadius, -SelfLoopRadius - SelfLoopYOffset);

        DrawBezier(handle, start, control1, control2, end, color);

        var arrowBase = CalculateCubicBezierPoint(BezierArrowOffsetT, start, control1, control2, end);
        var arrowTip = CalculateCubicBezierPoint(BezierArrowTipT, start, control1, control2, end);
        DrawArrowHead(handle, arrowBase, arrowTip, color);

        return CalculateCubicBezierPoint(0.5f, start, control1, control2, end);
    }

    private static Vector2 DrawEdge(
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
            GetBackwardBezierPoints(start, end, out var adjustedStart, out var control1, out var control2, out var adjustedEnd);
            DrawBezier(handle, adjustedStart, control1, control2, adjustedEnd, color);

            var arrowBase = CalculateCubicBezierPoint(BezierArrowOffsetT, adjustedStart, control1, control2, adjustedEnd);
            var arrowTip = CalculateCubicBezierPoint(BezierArrowTipT, adjustedStart, control1, control2, adjustedEnd);
            DrawArrowHead(handle, arrowBase, arrowTip, color);

            return CalculateCubicBezierPoint(0.5f, adjustedStart, control1, control2, adjustedEnd);
        }

        if (!PathIntersectsAnything([start, end], nodePositions, existingEdges))
        {
            DrawLineSegment(handle, start, end, existingEdges, color, isFinal: true);
            return (start + end) / 2;
        }

        // multi layer edge routing
        if (layerDiff > 1)
        {
            var points = new Vector2[layerDiff + 1];
            points[0] = start;
            for (var i = 1; i < layerDiff; i++)
            {
                var y = start.Y + (end.Y - start.Y) * i / layerDiff;
                points[i] = new Vector2(start.X, y);
            }
            points[layerDiff] = end;

            if (!PathIntersectsAnything(points, nodePositions, existingEdges))
            {
                for (var i = 0; i < points.Length - 1; i++)
                    DrawLineSegment(handle, points[i], points[i + 1], existingEdges, color, i == points.Length - 2);

                return GetMidpointOfPolyline(points);
            }
        }

        var match = from.Edges.FirstOrDefault(e => e.Connection?.Equals(to.Id) == true);
        if (match == null)
        {
            DrawLineSegment(handle, start, end, existingEdges, color, isFinal: true);
            return (start + end) / 2;
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
            return GetMidpointOfPolyline(elbowPath);
        }

        // raw direct
        DrawLineSegment(handle, start, end, existingEdges, color, isFinal: true);
        return (start + end) / 2;
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

    private void DrawNodeIcon(DrawingHandleScreen handle, SurgeryNode node, Vector2 pos)
    {
        if (!_nodeIcons.TryGetValue(node, out var icons))
        {
            icons = [];
            icons.EnsureCapacity(node.Special.Count);
            foreach (var special in node.Special)
            {
                if (special.GetIcon(_receiver!.Value, _body, _limb, _bodyPart, out var sprite))
                    icons.Add(_sprite.Frame0(sprite));
            }

            _nodeIcons[node] = icons;
        }

        // prevent division by 0
        if (!icons.Any())
        {
            return;
        }
        else if (icons.Count == 1) // special case so it doesnt look ass
        {
            var icon = icons[0];
            var centerPos = pos - icon.Size / 2;
            handle.DrawTexture(icon, centerPos);
        }
        else
        {
            var angleStep = MathF.Tau / icons.Count;

            for (var i = 0; i < icons.Count; i++)
            {
                var icon = icons[i];
                var angle = i * angleStep;

                var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * NodeRadius;
                var iconPos = pos + offset - icon.Size / 2;

                handle.DrawTexture(icon, iconPos);
            }
        }
    }

    private void DrawEdgeIcon(DrawingHandleScreen handle, SurgeryEdge edge, Vector2 pos, Color color)
    {
        if (!_edgeIcons.TryGetValue(edge, out var icon))
        {
            var sprite = edge.Requirement.GetIcon(_receiver!.Value, _body, _limb, _bodyPart);
            icon = sprite == null ? null : _sprite.Frame0(sprite);
            _edgeIcons[edge] = icon;
        }

        if (icon == null)
            return;

        var textureCenter = pos - icon.Size / 2;

        handle.DrawRect(UIBox2.FromDimensions(textureCenter, icon.Size), color);
        handle.DrawTexture(icon, textureCenter);
    }
}
