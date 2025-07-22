using Content.Shared.Surgery;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using System.Linq;
using System.Numerics;

namespace Content.Client.Surgery.UI;

public sealed partial class SurgeryGraphControl : Control
{
    #region Constants & Config

    private const float NodeRadius = 15f;
    private const float LayerHeight = 80f;
    private const float NodeSpacing = 60f;
    private const float EdgeArrowSize = 5f;
    private const float EdgeClearance = 8f;
    private const float BranchSpacing = 20f;
    private const float LayoutPadding = 20f;

    private const int BezierSegments = 24;
    private const float BezierArrowOffsetT = 0.95f;
    private const float BezierArrowTipT = 1.0f;
    private const float BackwardEdgeCurveHeight = LayerHeight / 2f;
    private const float BackwardEdgeControlOffsetX = 40f;

    private const float SelfLoopRadius = 20f;
    private const float SelfLoopYOffset = 20f;
    private const int SelfLoopSegments = 12;
    private const int SelfLoopArrowSegment = 3;

    private const float EdgeHoverDetectionWidth = 8f;
    private const float CurvedEdgeHoverWidthMultiplier = 1.5f;
    private const float BezierEarlyRejectionMultiplier = 2f;

    private const float ScrollSensitivity = 8f;
    private const float ScrollSensitivityMultiplier = 1 / ScrollSensitivity;
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 4;

    private static readonly Color NodeColor = Color.SkyBlue;
    private static readonly Color NodeHighlightColor = Color.SeaGreen;
    private static readonly Color CurrentNodeColor = Color.MediumPurple;
    private static readonly Color NodeHoverColor = Color.IndianRed;
    private static readonly Color EdgeColor = Color.PaleTurquoise;
    private static readonly Color EdgeHighlightColor = Color.GreenYellow;
    private static readonly Color EdgeHoverColor = Color.MediumVioletRed;

    #endregion

    #region Fields

    private SurgeryGraph? _graph;
    private Dictionary<SurgeryNode, int>? _layerMap;
    private Dictionary<int, List<SurgeryNode>>? _orderedLayers;
    private Dictionary<SurgeryNode, Vector2>? _nodePositions;

    public SurgeryNode? CurrentNode;

    [ViewVariables]
    public Vector2 GraphOffset = Vector2.Zero;

    [ViewVariables]
    public Vector2 Scale = Vector2.One;

    private bool _dragging = false;

    public HashSet<SurgeryNode> HighlightedNodes = [];

    private SurgeryNode? _hoveredNode;
    private SurgeryEdge? _hoveredEdge;

    private Matrix3x2 Transform => Matrix3x2.CreateTranslation(GraphOffset) * Matrix3x2.CreateScale(Scale);
    private Matrix3x2 InverseTransform => Matrix3x2.Invert(Transform, out var inverse) ? inverse : Matrix3x2.Identity;

    #endregion

    #region Actions

    public event Action<SurgeryNode>? NodeClicked;
    public event Action<SurgeryEdge>? EdgeClicked;

    #endregion

    #region Initialization

    public SurgeryGraphControl() : base()
    {
        IoCManager.InjectDependencies(this);

        MouseFilter = MouseFilterMode.Stop;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function == EngineKeyFunctions.UIRightClick)
            _dragging = true;
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            var graphPos = Vector2.Transform(args.RelativePixelPosition, InverseTransform);

            if (GetNodeAtPosition(graphPos) is { } node)
            {
                NodeClicked?.Invoke(node);
                return;
            }

            if (GetEdgeAtPosition(graphPos) is { } edge)
            {
                EdgeClicked?.Invoke(edge);
                return;
            }
        }

        if (args.Function == EngineKeyFunctions.UIRightClick)
            _dragging = false;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (_dragging)
        {
            GraphOffset += args.Relative / Scale;
            InvalidateMeasure();
            return;
        }

        var graphPos = Vector2.Transform(args.RelativePixelPosition, InverseTransform);

        _hoveredNode = GetNodeAtPosition(graphPos);
        _hoveredEdge = _hoveredNode == null ? GetEdgeAtPosition(graphPos) : null;
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);

        var cursorGraphPosBeforeZoom = (args.RelativePixelPosition - GraphOffset) / Scale;

        var delta = new Vector2(args.Delta.Y, args.Delta.Y) * ScrollSensitivityMultiplier;
        Scale += delta;

        Scale = new Vector2(
            Math.Clamp(Scale.X, MinZoom, MaxZoom),
            Math.Clamp(Scale.Y, MinZoom, MaxZoom)
        );

        GraphOffset = args.RelativePosition - cursorGraphPosBeforeZoom * Scale;
        InvalidateMeasure();
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        if (_nodePositions == null || !_nodePositions.Any())
            return Vector2.Zero;

        var bounds = Box2.Empty;

        foreach (var pos in _nodePositions.Values)
        {
            var nodeBox = Box2.FromDimensions(
                pos - new Vector2(NodeRadius),
                new Vector2(NodeRadius * 2)
            );

            bounds = bounds.Union(nodeBox);
        }

        var desiredSize = bounds.Translated(GlobalPosition).Scale(Scale).Size;

        return new Vector2(
            MathF.Min(desiredSize.X, availableSize.X),
            MathF.Min(desiredSize.Y, availableSize.Y)
        );
    }

    public void ChangeGraph(SurgeryGraph? graph)
    {
        if (_graph == graph)
            return;

        _graph = graph;

        if (_graph == null)
        {
            _nodePositions = null;
            return;
        }

        _layerMap = AssignLayers(_graph);
        _orderedLayers = ReduceCrossings(_layerMap, _graph);
        _nodePositions = AssignCoordinates(_orderedLayers);
        InvalidateMeasure();
    }

    #endregion

    #region Layout Logic

    private static Dictionary<SurgeryNode, int> AssignLayers(SurgeryGraph graph)
    {
        Dictionary<SurgeryNode, int> layers = [];
        HashSet<SurgeryNode> visited = [];

        foreach (var node in graph.Nodes.Values)
        {
            if (visited.Contains(node))
                continue;

            Queue<(SurgeryNode node, int layer)> queue = [];
            queue.Enqueue((node, 0));

            while (queue.Count > 0)
            {
                var (current, layer) = queue.Dequeue();

                if (visited.Contains(current))
                    continue;

                visited.Add(current);
                layers[current] = layer;

                foreach (var edge in current.Edges)
                {
                    if (edge.Connection == null || !graph.Nodes.TryGetValue(edge.Connection.Value, out var target))
                        continue;

                    if (!visited.Contains(target))
                        queue.Enqueue((target, layer + 1));
                }
            }
        }

        return layers;
    }

    private static Dictionary<int, List<SurgeryNode>> ReduceCrossings(Dictionary<SurgeryNode, int> layers, SurgeryGraph graph)
    {
        Dictionary<int, List<SurgeryNode>> ordered = [];

        foreach (var (node, layer) in layers)
        {
            if (!ordered.ContainsKey(layer))
                ordered[layer] = [];
            ordered[layer].Add(node);
        }

        for (var i = 1; ordered.ContainsKey(i); i++)
        {
            var layer = ordered[i];
            layer.Sort((a, b) =>
            {
                float GetBarycenter(SurgeryNode node)
                {
                    List<int> indices = [];
                    foreach (var edge in node.Edges)
                    {
                        if (edge.Connection == null)
                            continue;

                        if (!graph.Nodes.TryGetValue(edge.Connection.Value, out var parent))
                            continue;

                        if (!layers.TryGetValue(parent, out var parentLayer) || parentLayer != i - 1)
                            continue;

                        var index = ordered[i - 1].IndexOf(parent);
                        if (index != -1)
                            indices.Add(index);
                    }
                    return indices.Count > 0 ? (float)indices.Average() : 0f;
                }

                return GetBarycenter(a).CompareTo(GetBarycenter(b));
            });
        }

        return ordered;
    }

    private static Dictionary<SurgeryNode, Vector2> AssignCoordinates(Dictionary<int, List<SurgeryNode>> orderedLayers)
    {
        Dictionary<SurgeryNode, Vector2> positions = [];

        foreach (var (layerIndex, nodes) in orderedLayers)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                var x = LayoutPadding + i * NodeSpacing;
                var y = LayoutPadding + layerIndex * LayerHeight;
                positions[nodes[i]] = new Vector2(x, y);
            }
        }

        return positions;
    }

    #endregion

    #region Drawing

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_graph == null || _nodePositions == null || _layerMap == null)
            return;

        List<Vector2[]> drawnEdges = [];
        HashSet<(SurgeryNode, SurgeryNode)> drawnPairs = [];

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
                if (edge.Connection == null || !_graph.Nodes.TryGetValue(edge.Connection.Value, out var target))
                    continue;

                if (!_nodePositions.TryGetValue(target, out var targetPos))
                    continue;

                var key = (node, target);
                if (drawnPairs.Contains(key) || drawnPairs.Contains((target, node)))
                    continue;

                drawnPairs.Add(key);

                var isHighlighted = HighlightedNodes.Contains(node) && HighlightedNodes.Contains(target);
                var edgeColor = _hoveredEdge == edge
                    ? EdgeHoverColor
                    : isHighlighted
                        ? EdgeHighlightColor
                        : EdgeColor;

                if (node == target)
                    DrawSelfLoop(handle, pos, edgeColor);
                else
                    DrawEdge(handle, pos, targetPos, node, target, edgeColor, _layerMap, _nodePositions, drawnEdges);
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
        var points = new Vector2[SelfLoopSegments + 1];
        for (var i = 0; i <= SelfLoopSegments; i++)
        {
            var angle = 2 * MathF.PI * i / SelfLoopSegments;
            points[i] = pos + new Vector2(
                SelfLoopRadius * MathF.Sin(angle),
                -SelfLoopYOffset - SelfLoopRadius * MathF.Cos(angle)
            );
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, points, color);
        DrawArrowHead(handle, points[SelfLoopArrowSegment - 1], points[SelfLoopArrowSegment], color);
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

    #endregion

    #region Geometry Helpers

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
                if (edge.Connection == null || !_graph.Nodes.TryGetValue(edge.Connection.Value, out var target))
                    continue;

                if (!_nodePositions.TryGetValue(target, out var targetPos))
                    continue;

                if (IsPointOnEdge(position, nodePos, targetPos, node, target, _layerMap))
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
            var control1 = new Vector2(start.X - BackwardEdgeControlOffsetX * (1 + (fromLayer - toLayer)), start.Y - BackwardEdgeCurveHeight * (1 + (fromLayer - toLayer)));
            var control2 = new Vector2(end.X - BackwardEdgeControlOffsetX * (1 + (fromLayer - toLayer)), end.Y - BackwardEdgeCurveHeight * (1 + (fromLayer - toLayer)));
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

    #endregion
}
