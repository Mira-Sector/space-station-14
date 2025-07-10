using System.Numerics;
using Content.Shared.Surgery;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client.Surgery.UI;

public sealed partial class SurgeryGraphControl : Control
{
    private SurgeryGraph? _graph;

    private const float NodeCircleRadius = 10f;
    private const float NodeDistance = 100f;

    public SurgeryGraphControl()
    {
        IoCManager.InjectDependencies(this);
    }

    public void ChangeGraph(SurgeryGraph? graph)
    {
        if (_graph == graph)
            return;

        _graph = graph;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_graph is not { } graph)
            return;

        Dictionary<SurgeryNode, Vector2> nodePositions = [];
        var previousNode = graph.Nodes[graph.StartingNode];

        var screenCenter = PixelSize / 2;
        nodePositions[previousNode] = screenCenter;

        Dictionary<SurgeryNode, HashSet<SurgeryNode>> nodeMap = [];
        var nodes = new Queue<SurgeryNode>();
        nodes.Enqueue(previousNode);

        while (nodes.TryDequeue(out var node))
        {
            var angleStep = Angle.FromDegrees(360f / node.Edges.Count);
            var currentAngle = Angle.Zero;

            foreach (var edge in node.Edges)
            {
                if (edge.Connection == null)
                    continue;

                var connectedNode = graph.Nodes[edge.Connection.Value];
                if (!nodePositions.ContainsKey(connectedNode))
                {
                    var offset = new Vector2(
                        NodeDistance * MathF.Cos((float)currentAngle.Theta),
                        NodeDistance * MathF.Sin((float)currentAngle.Theta));

                    nodePositions[connectedNode] = nodePositions[node] + offset;
                    nodes.Enqueue(connectedNode);

                    if (!nodeMap.TryGetValue(node, out var nodeConnections))
                    {
                        nodeConnections = [];
                        nodeMap[node] = nodeConnections;
                    }
                    nodeConnections.Add(connectedNode);
                    currentAngle += angleStep;
                }
            }
        }

        foreach (var (node, connections) in nodeMap)
        {
            if (!nodePositions.TryGetValue(node, out var startPos))
                continue;

            foreach (var connectedNode in connections)
            {
                if (nodePositions.TryGetValue(connectedNode, out var endPos))
                    DrawEdge(handle, startPos, endPos);
            }
        }

        foreach (var (node, position) in nodePositions)
            DrawNode(handle, node, position);
    }

    private void DrawNode(DrawingHandleScreen handle, SurgeryNode node, Vector2 position)
    {
        handle.DrawCircle(position, NodeCircleRadius, Color.White, false);
    }

    private void DrawEdge(DrawingHandleScreen handle, Vector2 startPos, Vector2 endPos)
    {
        handle.DrawLine(startPos, endPos, Color.Gray);

        var direction = (endPos - startPos).Normalized();
        var perpendicular = new Vector2(-direction.Y, direction.X);
        var arrowSize = 5f;

        var arrowPoint1 = endPos - direction * NodeCircleRadius + perpendicular * arrowSize;
        var arrowPoint2 = endPos - direction * NodeCircleRadius - perpendicular * arrowSize;

        handle.DrawLine(endPos, arrowPoint1, Color.Gray);
        handle.DrawLine(endPos, arrowPoint2, Color.Gray);
    }
}
