using Content.Shared.Surgery;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using System.Linq;
using System.Numerics;

namespace Content.Client.Surgery.UI;

public sealed partial class SurgeryGraphControl : Control
{
    private const float NodeCircleRadius = 15f;
    private const float LayerHeight = 80f;
    private const float NodeSpacing = 60f;

    private static readonly Color HighlightedNodeColor = Color.SeaGreen;
    private static readonly Color NormalNodeColor = Color.SkyBlue;

    private const float EdgeArrowSize = 5f;
    private static readonly Color EdgeColor = Color.PaleTurquoise;

    private const float SelfEdgeLoopRadius = 20f;
    private const int SelfEdgeSegments = 12;
    private const float SelfEdgeVerticalOffset = 20f;
    private const int SelfEdgeArrowPositionSegment = 3;

    private const float EdgeClearance = 8f;
    private const float BranchSpacing = 20f;
    private const int MaxRoutingAttempts = 5;

    private const float LayoutPadding = 20f;

    private SurgeryGraph? _graph;
    private Dictionary<SurgeryNode, Vector2>? _nodePositions;
    private Dictionary<SurgeryNode, int>? _layers;
    public HashSet<SurgeryNode> HighlightedNodes = [];

    public SurgeryGraphControl()
    {
        IoCManager.InjectDependencies(this);
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

        _layers = AssignLayers(_graph);
        var ordered = ReduceCrossings(_layers, _graph);
        _nodePositions = AssignCoordinates(ordered);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        if (_graph == null || _nodePositions == null || _layers == null)
            return;

        foreach (var (node, position) in _nodePositions)
            DrawNode(handle, node, position);

        List<Vector2[]> drawnEdges = [];
        HashSet<(SurgeryNode, SurgeryNode)> drawnEdgePairs = [];
        foreach (var (node, position) in _nodePositions)
        {
            foreach (var edge in node.Edges)
            {
                if (edge.Connection == null || !_graph.Nodes.TryGetValue(edge.Connection.Value, out var targetNode))
                    continue;

                if (!_nodePositions.TryGetValue(targetNode, out var targetPos))
                    continue;

                var edgeKey = (node, targetNode);
                var reverseKey = (targetNode, node);
                if (drawnEdgePairs.Contains(edgeKey) || drawnEdgePairs.Contains(reverseKey))
                    continue;

                drawnEdgePairs.Add(edgeKey);

                if (node == targetNode)
                    DrawSelfReferentialEdge(handle, position);
                else
                    DrawEdge(handle, position, targetPos, node, targetNode, _layers, _nodePositions, drawnEdges);
            }
        }
    }

    private static Dictionary<SurgeryNode, int> AssignLayers(SurgeryGraph graph)
    {
        Dictionary<SurgeryNode, int> layers = [];
        HashSet<SurgeryNode> visited = [];

        foreach (var start in graph.Nodes.Values)
        {
            if (visited.Contains(start))
                continue;

            Queue<(SurgeryNode node, int layer)> queue = new();
            queue.Enqueue((start, 0));

            while (queue.Count > 0)
            {
                var (node, layer) = queue.Dequeue();

                if (visited.Contains(node))
                    continue;

                visited.Add(node);
                layers[node] = layer;

                foreach (var edge in node.Edges)
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
        Dictionary<int, List<SurgeryNode>> orderedLayers = [];

        foreach (var (node, layer) in layers)
        {
            if (!orderedLayers.ContainsKey(layer))
                orderedLayers[layer] = [];
            orderedLayers[layer].Add(node);
        }

        for (var i = 1; orderedLayers.ContainsKey(i); i++)
        {
            var layer = orderedLayers[i];
            layer.Sort((a, b) =>
            {
                float GetBarycenter(SurgeryNode node)
                {
                    var parentCenters = node.Edges
                        .Where(e => e.Connection != null &&
                                    graph.Nodes.TryGetValue(e.Connection.Value, out var parent) &&
                                    layers[parent] == i - 1)
                        .Select(e => orderedLayers[i - 1].IndexOf(graph.Nodes[e.Connection!.Value]));

                    return parentCenters.Any() ? (float)parentCenters.Average() : 0;
                }

                return GetBarycenter(a).CompareTo(GetBarycenter(b));
            });
        }

        return orderedLayers;
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

    private static void DrawEdge(DrawingHandleScreen handle, Vector2 startPos, Vector2 endPos, SurgeryNode from, SurgeryNode to, Dictionary<SurgeryNode, int> layers, Dictionary<SurgeryNode, Vector2> nodePositions, List<Vector2[]> existingEdges)
    {
        var direction = (endPos - startPos).Normalized();
        var start = startPos + direction * (NodeCircleRadius + EdgeClearance);
        var end = endPos - direction * (NodeCircleRadius + EdgeClearance);

        Vector2[] straightPath = [start, end];
        if (!PathIntersectsAnything(straightPath, nodePositions, existingEdges))
        {
            DrawLineSegment(handle, start, end, existingEdges, isFinal: true);
            return;
        }

        var mid = new Vector2(start.X, end.Y);
        Vector2[] elbowPath = [start, mid, end];
        if (!PathIntersectsAnything(elbowPath, nodePositions, existingEdges))
        {
            DrawLineSegment(handle, start, mid, existingEdges);
            DrawLineSegment(handle, mid, end, existingEdges, isFinal: true);
            return;
        }

        for (var attempt = 0; attempt < MaxRoutingAttempts; attempt++)
        {
            var offset = BranchSpacing * (attempt + 1);
            foreach (var side in new[] { 1, -1 })
            {
                var dx = side * offset;

                var mid1 = new Vector2(start.X + dx, start.Y);
                var mid2 = new Vector2(start.X + dx, end.Y);
                var path = new[] { start, mid1, mid2, end };

                if (!PathIntersectsAnything(path, nodePositions, existingEdges))
                {
                    DrawLineSegment(handle, start, mid1, existingEdges);
                    DrawLineSegment(handle, mid1, mid2, existingEdges);
                    DrawLineSegment(handle, mid2, end, existingEdges, isFinal: true);
                    return;
                }
            }
        }

        DrawLineSegment(handle, start, end, existingEdges, isFinal: true);
    }

    private static bool PathIntersectsAnything(Vector2[] path, Dictionary<SurgeryNode, Vector2> nodePositions, List<Vector2[]> existingEdges)
    {
        foreach (var nodePos in nodePositions.Values)
        {
            for (var i = 0; i < path.Length - 1; i++)
            {
                var start = path[i];
                var end = path[i + 1];

                // if perfectly vertical or horizontal skip node collision check
                if (MathF.Abs(start.X - end.X) < 0.5f || MathF.Abs(start.Y - end.Y) < 0.5f)
                    continue;

                if (PointLineDistance(start, end, nodePos) < NodeCircleRadius + EdgeClearance)
                    return true;
            }
        }

        foreach (var edge in existingEdges)
        {
            for (var i = 0; i < path.Length - 1; i++)
            {
                if (LinesIntersect(path[i], path[i + 1], edge[0], edge[1]))
                    return true;
            }
        }

        return false;
    }

    private static void DrawLineSegment(DrawingHandleScreen handle, Vector2 start, Vector2 end, List<Vector2[]> existingEdges, bool isFinal = false)
    {
        handle.DrawLine(start, end, EdgeColor);
        existingEdges.Add([start, end]);

        if (isFinal)
            DrawArrowHead(handle, start, end);
    }

    private static bool LinesIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        var orientation1 = (b1.X - a1.X) * (a2.Y - a1.Y) - (b1.Y - a1.Y) * (a2.X - a1.X);
        var orientation2 = (b2.X - a1.X) * (a2.Y - a1.Y) - (b2.Y - a1.Y) * (a2.X - a1.X);
        var orientation3 = (a1.X - b1.X) * (b2.Y - b1.Y) - (a1.Y - b1.Y) * (b2.X - b1.X);
        var orientation4 = (a2.X - b1.X) * (b2.Y - b1.Y) - (a2.Y - b1.Y) * (b2.X - b1.X);

        if (orientation1 * orientation2 < 0 && orientation3 * orientation4 < 0)
            return true;

        if (orientation1 == 0 && OnSegment(a1, a2, b1))
            return true;
        if (orientation2 == 0 && OnSegment(a1, a2, b2))
            return true;
        if (orientation3 == 0 && OnSegment(b1, b2, a1))
            return true;
        if (orientation4 == 0 && OnSegment(b1, b2, a2))
            return true;

        return false;
    }

    private static bool OnSegment(Vector2 p, Vector2 q, Vector2 r)
    {
        return r.X <= Math.Max(p.X, q.X) &&
               r.X >= Math.Min(p.X, q.X) &&
               r.Y <= Math.Max(p.Y, q.Y) &&
               r.Y >= Math.Min(p.Y, q.Y);
    }

    private static float PointLineDistance(Vector2 start, Vector2 end, Vector2 point)
    {
        var l2 = (end - start).LengthSquared();
        if (l2 == 0)
            return Vector2.Distance(point, start);

        var t = MathF.Max(0, MathF.Min(1, Vector2.Dot(point - start, end - start) / l2));
        var projection = start + t * (end - start);
        return Vector2.Distance(point, projection);
    }

    private static void DrawArrowHead(DrawingHandleScreen handle, Vector2 startPos, Vector2 endPos)
    {
        var direction = (endPos - startPos).Normalized();
        var perpendicular = new Vector2(-direction.Y, direction.X);

        var arrowBase = endPos - direction * NodeCircleRadius;
        var arrowPoint1 = arrowBase + perpendicular * EdgeArrowSize - direction * EdgeArrowSize;
        var arrowPoint2 = arrowBase - perpendicular * EdgeArrowSize - direction * EdgeArrowSize;

        handle.DrawLine(endPos, arrowPoint1, EdgeColor);
        handle.DrawLine(endPos, arrowPoint2, EdgeColor);
    }

    private void DrawNode(DrawingHandleScreen handle, SurgeryNode node, Vector2 position)
    {
        var color = HighlightedNodes.Contains(node) ? HighlightedNodeColor : NormalNodeColor;
        handle.DrawCircle(position, NodeCircleRadius, color, true);
    }

    private static void DrawSelfReferentialEdge(DrawingHandleScreen handle, Vector2 nodePosition)
    {
        var points = new Vector2[SelfEdgeSegments + 1];

        for (var i = 0; i <= SelfEdgeSegments; i++)
        {
            var angle = 2 * MathF.PI * i / SelfEdgeSegments;
            points[i] = nodePosition + new Vector2(
                SelfEdgeLoopRadius * MathF.Sin(angle),
                -SelfEdgeVerticalOffset - SelfEdgeLoopRadius * MathF.Cos(angle)
            );
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, points, EdgeColor);

        DrawArrowHead(handle, points[SelfEdgeArrowPositionSegment - 1], points[SelfEdgeArrowPositionSegment]);
    }
}
