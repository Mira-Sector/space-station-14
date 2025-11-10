using Content.Shared.Arcade.Racer.Stage;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.Arcade.Racer;

public sealed partial class RacerEditorViewportControl
{
    private static List<Vector2> SampleBezier(List<Vector2> points, int resolution)
    {
        List<Vector2> result = new(resolution + 1);
        for (var i = 0; i <= resolution; i++)
        {
            var t = i / (float)resolution;
            result.Add(EvaluateBezier(points, t));
        }
        return result;
    }

    private static Vector2 EvaluateBezier(List<Vector2> pts, float t)
    {
        var temp = pts.ToList();
        for (var k = pts.Count - 1; k > 0; k--)
        {
            for (var i = 0; i < k; i++)
                temp[i] = Vector2.Lerp(temp[i], temp[i + 1], t);
        }
        return temp[0];
    }

    private bool TryGetNodeAtPosition(Vector2 position, [NotNullWhen(true)] out string? nodeId, [NotNullWhen(true)] out RacerArcadeStageNode? node)
    {
        if (_data is not { } data)
        {
            nodeId = null;
            node = null;
            return false;
        }

        foreach (var (id, x) in data.Graph.Nodes)
        {
            if (Vector2.Distance(position, x.Position) > NodeRadius)
                continue;

            nodeId = id;
            node = x;
            return true;
        }

        nodeId = null;
        node = null;
        return false;
    }

    private bool TryGetEdgeAtPosition(Vector2 position, [NotNullWhen(true)] out IRacerArcadeStageEdge? edge, [NotNullWhen(true)] out Vector2? nearestPoint)
    {
        edge = null;
        nearestPoint = Vector2.Zero;

        if (_data is not { } data)
            return false;

        var closestDistance = float.MaxValue;

        foreach (var (connection, node) in data.Graph.GetConnections())
        {
            if (!data.Graph.TryGetNextNode(connection, out var nextNode))
                continue;

            if (connection is IRacerArcadeStageRenderableEdge renderableEdge)
            {
                var points = GetWorldSpaceEdgePoints(renderableEdge, node.Position, nextNode.Position);
                var sampled = SampleBezier(points, RenderableEdgeBezierSamples);
                for (var i = 1; i < sampled.Count; i++)
                {
                    var prev = sampled[i - 1];
                    var next = sampled[i];

                    var dist = DistanceFromPointToSegment(position, prev, next);
                    if (dist > closestDistance)
                        continue;

                    closestDistance = dist;
                    edge = renderableEdge;
                    nearestPoint = Vector2.Lerp(prev, next, 0.5f);
                }
            }
            else
            {
                var dist = DistanceFromPointToSegment(position, node.Position, nextNode.Position);
                if (dist > closestDistance)
                    continue;

                closestDistance = dist;
                edge = connection;
                nearestPoint = Vector2.Lerp(node.Position, nextNode.Position, 0.5f);
            }
        }

        // only pick edges which are close
        if (edge != null && closestDistance < EdgeSelectThreshold)
            return true;

        return false;
    }

    private void CreateNode(Vector2 position)
    {
        if (_data is not { } data)
            return;

        position = GetClosestGridPoint(position);

        var popup = new RacerEditorViewportNewNodePopup();
        popup.OnNewNodeName += args =>
        {
            if (data.Graph.Nodes.ContainsKey(args))
                return;

            var node = new RacerArcadeStageNode()
            {
                Position = position,
                Connections = []
            };
            data.Graph.Nodes[args] = node;

            // for convenience
            // you literally never want to JUST add a node
            // first node is pointless however
            if (data.Graph.Nodes.Count > 1)
                EditNode(args, node);
        };
        AddPopup(popup);
    }

    private void EditNode(string id, RacerArcadeStageNode node)
    {
        if (_data is not { } data)
            return;

        var popup = new RacerEditorViewportEditNodePopup(id, node, data.Graph, _prototype);
        popup.OnNodeEdited += newNode =>
        {
            data.Graph.Nodes[id] = newNode;
        };
        AddPopup(popup);
    }

    private void DeleteNode(string id)
    {
        if (_data is not { } data)
            return;

        if (!data.Graph.Nodes.Remove(id))
            return;

        Dictionary<RacerArcadeStageNode, List<IRacerArcadeStageEdge>> toRemove = [];
        foreach (var (edge, node) in data.Graph.GetConnections())
        {
            if (edge is not RacerArcadeStageEdgeNode edgeNode)
                continue;

            if (edgeNode.ConnectionId != id)
                continue;

            if (!toRemove.TryGetValue(node, out var edges))
            {
                edges = [];
                toRemove[node] = edges;
            }
            edges.Add(edge);
        }

        foreach (var (node, edges) in toRemove)
        {
            foreach (var edge in edges)
                node.Connections.Remove(edge);
        }
    }

    private void AddControlPoint(IRacerArcadeStageRenderableEdge edge, Vector2 worldPosition)
    {
        if (_data is not { } data)
            return;

        if (!data.Graph.TryGetParentNode(edge, out var node))
            return;

        var localPos = worldPosition - node.Position;

        var insertIndex = 0;
        var bestDistance = float.MaxValue;
        var z = 0f;
        for (var i = 0; i < edge.ControlPoints.Count; i++)
        {
            var current = edge.ControlPoints[i];
            var next = i + 1 < edge.ControlPoints.Count ? edge.ControlPoints[i + 1] : new Vector3(0f, 0f, current.Z);

            var mid = (current + next) / 2f;
            var dist = Vector2.Distance(localPos, mid.Xy);
            if (dist > bestDistance)
                continue;

            bestDistance = dist;
            insertIndex = i + 1;
            z = mid.Z;
        }

        var cp = new Vector3(localPos.X, localPos.Y, z);
        edge.ControlPoints.Insert(insertIndex, cp);
    }

    private void AddPopup(RacerEditorViewportPopup popup)
    {
        if (_popup is { } oldPopup)
        {
            oldPopup.Close();
            RemoveChild(oldPopup);
        }

        _popup = popup;
        var box = UIBox2.FromDimensions(UserInterfaceManager.MousePositionScaled.Position, Vector2.One);
        popup.Open(box);
        AddChild(popup);
    }

    private void GridSizeStep(bool positive)
    {
        if (!positive && GridSize <= 0)
            return;

        var log = MathF.Log2(GridSize);
        var adjust = positive ? 1 : -1;
        var newGrid = (uint)MathF.Pow(2f, MathF.Floor(log) + adjust);
        SetGridSize(newGrid);
    }

    private Vector2 GetClosestGridPoint(Vector2 pos)
    {
        var closestX = MathF.Round(pos.X / GridSize) * GridSize;
        var closestY = MathF.Round(pos.Y / GridSize) * GridSize;
        return new Vector2(closestX, closestY);
    }

    private static float DistanceFromPointToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var ap = point - a;
        var t = Math.Clamp(Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab), 0f, 1f);
        var closest = a + ab * t;
        return Vector2.Distance(point, closest);
    }

    private static List<Vector2> GetWorldSpaceEdgePoints(IRacerArcadeStageRenderableEdge edge, Vector2 sourceNode, Vector2 nextNode)
    {
        List<Vector2> points = new(edge.ControlPoints.Count + 2);
        points.Add(sourceNode);

        if (edge.ControlPoints.Any())
        {
            foreach (var cp in edge.ControlPoints)
                points.Add(cp.Xy + sourceNode);
        }
        points.Add(nextNode);
        return points;
    }
}
