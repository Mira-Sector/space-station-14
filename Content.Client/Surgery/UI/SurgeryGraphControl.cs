using Content.Shared.Surgery;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using System.Numerics;

namespace Content.Client.Surgery.UI;

public sealed partial class SurgeryGraphControl : Control
{
    private const float NodeCircleRadius = 20f;
    private const float NodeDistance = 100f;

    private static readonly Color HighlightedNodeColor = Color.Red;
    private static readonly Color NormalNodeColor = Color.Blue;

    private const float EdgeArrowSize = 5f;
    private static readonly Color EdgeColor = Color.Gray;

    private const float SelfEdgeLoopRadius = 20f;
    private const int SelfEdgeSegments = 12;
    private const float SelfEdgeVerticalOffset = 20f;
    private const int SelfEdgeArrowPositionSegment = 3;

    private const int CurveSegments = 8;
    private const float CurveHeight = 30f;

    private const int MaxIterations = 100;
    private const float RepulsionForce = 1000f;
    private const float SpringLength = NodeDistance;
    private const float SpringConstant = 0.05f;
    private const float Damping = 0.9f;
    private const float Temperature = 100f;

    private const float LayoutPadding = 20f;

    private SurgeryGraph? _graph;
    private Dictionary<SurgeryNode, Vector2>? _nodePositions;
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
        }
        else
        {
            _nodePositions = InitializeNodePositions();

            Dictionary<SurgeryNode, Vector2> velocities = [];
            for (var i = 0; i < MaxIterations; i++)
                ApplyForces(_graph, _nodePositions, velocities, Temperature * (1 - (float)i / MaxIterations));
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        if (_graph == null || _nodePositions == null)
            return;

        HashSet<(SurgeryNode, SurgeryNode)> drawnEdges = [];
        foreach (var (node, position) in _nodePositions)
        {
            foreach (var edge in node.Edges)
            {
                if (edge.Connection == null || !_graph.Nodes.TryGetValue(edge.Connection.Value, out var targetNode))
                    continue;

                if (!_nodePositions.TryGetValue(targetNode, out var targetPos))
                    continue;

                var edgeKey = (node, targetNode);
                if (drawnEdges.Contains(edgeKey))
                    continue;

                drawnEdges.Add(edgeKey);

                if (node == targetNode)
                {
                    DrawSelfReferentialEdge(handle, position);
                }
                else
                {
                    var isBackEdge = IsBackEdge(node, targetNode, _graph);
                    DrawEdge(handle, position, targetPos, isBackEdge);
                }
            }
        }

        foreach (var (node, position) in _nodePositions)
            DrawNode(handle, node, position);
    }

    private Dictionary<SurgeryNode, Vector2> InitializeNodePositions()
    {
        var positions = new Dictionary<SurgeryNode, Vector2>();
        if (_graph == null || _graph.Nodes.Count == 0 || PixelSize.X <= 2 * LayoutPadding || PixelSize.Y <= 2 * LayoutPadding)
            return positions;

        var nodeCount = _graph.Nodes.Count;
        var columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(nodeCount)));
        var rows = Math.Max(1, (int)Math.Ceiling((float)nodeCount / columns));

        var safeColumns = Math.Max(2, columns);
        var safeRows = Math.Max(2, rows);

        var maxAllowedSpacingX = (PixelSize.X - 2 * LayoutPadding) / (safeColumns - 1);
        var maxAllowedSpacingY = (PixelSize.Y - 2 * LayoutPadding) / (safeRows - 1);
        var uniformSpacing = Math.Min(maxAllowedSpacingX, maxAllowedSpacingY);

        var offsetX = (PixelSize.X - (columns - 1) * uniformSpacing) / 2;
        var offsetY = (PixelSize.Y - (rows - 1) * uniformSpacing) / 2;

        var index = 0;
        foreach (var node in _graph.Nodes.Values)
        {
            var x = index % columns;
            var y = index / columns;

            positions[node] = new Vector2(
                offsetX + x * uniformSpacing,
                offsetY + y * uniformSpacing
            );

            index++;
        }

        return positions;
    }

    private void ApplyForces(SurgeryGraph graph, Dictionary<SurgeryNode, Vector2> positions, Dictionary<SurgeryNode, Vector2> velocities, float temperature)
    {
        foreach (var node in positions.Keys)
        {
            if (!velocities.ContainsKey(node))
                velocities[node] = Vector2.Zero;
        }

        foreach (var (nodeA, posA) in positions)
        {
            var totalForce = Vector2.Zero;

            foreach (var (nodeB, posB) in positions)
            {
                if (nodeA == nodeB)
                    continue;

                var delta = posA - posB;
                var distance = Math.Max(delta.Length(), float.Epsilon);

                var repulsion = RepulsionForce / (distance * distance);
                totalForce += repulsion * delta.Normalized();
            }

            velocities[nodeA] += totalForce;
        }

        foreach (var (node, pos) in positions)
        {
            foreach (var edge in node.Edges)
            {
                if (edge.Connection == null || !graph.Nodes.TryGetValue(edge.Connection.Value, out var target))
                    continue;

                if (!positions.TryGetValue(target, out var targetPos))
                    continue;

                var delta = targetPos - pos;
                var distance = Math.Max(delta.Length(), float.Epsilon);
                var direction = delta.Normalized();

                var displacement = distance - SpringLength;
                var springForce = SpringConstant * displacement;

                velocities[node] += springForce * direction;
                velocities[target] -= springForce * direction;
            }
        }

        foreach (var node in positions.Keys)
        {
            velocities[node] *= Damping;
            positions[node] += velocities[node];
        }

        var minX = LayoutPadding + NodeCircleRadius;
        var minY = LayoutPadding + NodeCircleRadius;
        var maxX = PixelSize.X - LayoutPadding - NodeCircleRadius;
        var maxY = PixelSize.Y - LayoutPadding - NodeCircleRadius;

        /*
        foreach (var node in positions.Keys)
        {
            var p = positions[node];
            positions[node] = new Vector2(
                Math.Clamp(p.X, minX, maxX),
                Math.Clamp(p.Y, minY, maxY)
            );
        }
        */
    }


    private static bool IsBackEdge(SurgeryNode from, SurgeryNode to, SurgeryGraph graph)
    {
        Queue<SurgeryNode> queue = [];
        HashSet<SurgeryNode> visited = [];

        queue.Enqueue(graph.Nodes[graph.StartingNode]);
        visited.Add(graph.Nodes[graph.StartingNode]);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var edge in current.Edges)
            {
                if (edge.Connection == null || !graph.Nodes.TryGetValue(edge.Connection.Value, out var neighbor))
                    continue;

                // normal forward edge
                if (neighbor == to && current == from)
                    return false;

                // back edge found
                if (neighbor == to)
                    return true;

                if (visited.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }

        return false;
    }

    private static void DrawEdge(DrawingHandleScreen handle, Vector2 startPos, Vector2 endPos, bool isBackEdge)
    {
        if (!isBackEdge)
        {
            handle.DrawLine(startPos, endPos, EdgeColor);
        }
        else
        {
            var points = new Vector2[CurveSegments + 1];
            var direction = (endPos - startPos).Normalized();
            var perpendicular = new Vector2(-direction.Y, direction.X);

            for (var i = 0; i <= CurveSegments; i++)
            {
                var t = i / (float)CurveSegments;
                var controlPoint = (startPos + endPos) / 2 + perpendicular * CurveHeight;
                points[i] = CalculateQuadraticBezierPoint(startPos, controlPoint, endPos, t);
            }

            handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, points, EdgeColor);
        }

        DrawArrowHead(handle, startPos, endPos);
    }

    private static Vector2 CalculateQuadraticBezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        var u = 1 - t;
        return u * u * p0 + 2 * u * t * p1 + t * t * p2;
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
        handle.DrawCircle(position, NodeCircleRadius, color, false);
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
