using Content.Shared.Arcade.Racer.Stage;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.Arcade.Racer;

public sealed partial class RacerEditorViewportControl
{
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
            if (Vector2.Distance(position, x.Position.Xy) > NodeRadius)
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
                var points = renderableEdge.GetWorldSpaceEdgePoints(node.Position, nextNode.Position);
                var sampled = RacerArcadeStageGraphHelpers.SampleBezier(points, RenderableEdgeBezierSamples);
                for (var i = 1; i < sampled.Length; i++)
                {
                    var prev = sampled[i - 1];
                    var next = sampled[i];

                    var dist = DistanceFromPointToSegment(position, prev.Xy, next.Xy);
                    if (dist > closestDistance)
                        continue;

                    closestDistance = dist;
                    edge = renderableEdge;
                    nearestPoint = Vector2.Lerp(prev.Xy, next.Xy, 0.5f);
                }
            }
            else
            {
                var dist = DistanceFromPointToSegment(position, node.Position.Xy, nextNode.Position.Xy);
                if (dist > closestDistance)
                    continue;

                closestDistance = dist;
                edge = connection;
                nearestPoint = Vector2.Lerp(node.Position.Xy, nextNode.Position.Xy, 0.5f);
            }
        }

        // only pick edges which are close
        if (edge != null && closestDistance < EdgeSelectThreshold)
            return true;

        return false;
    }

    private bool TryGetEdgeControlPointAtPosition(IRacerArcadeStageRenderableEdge edge, Vector2 position, [NotNullWhen(true)] out int? index, [NotNullWhen(true)] out Vector2? worldPos)
    {
        if (_data is not { } data)
        {
            index = null;
            worldPos = null;
            return false;
        }

        if (!data.Graph.TryGetParentNode(edge, out var parent))
        {
            index = null;
            worldPos = null;
            return false;
        }

        for (var i = 0; i < edge.ControlPoints.Length; i++)
        {
            var cp = edge.ControlPoints[i];
            var worldCp = cp.Xy + parent.Position.Xy;
            if (Vector2.Distance(position, worldCp) > ControlPointRadius)
                continue;

            index = i;
            worldPos = worldCp;
            return true;
        }

        index = null;
        worldPos = null;
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
                Position = new(position.X, position.Y, 0f),
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

    private void NodeHeightStep(RacerArcadeStageNode node, bool positive)
    {
        var adjust = positive ? GridSize : -GridSize;
        node.Position.Z += adjust;
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

        var localPos = worldPosition - node.Position.Xy;

        var insertIndex = 0;
        var bestDistance = float.MaxValue;
        var z = 0f;
        for (var i = 0; i < edge.ControlPoints.Length; i++)
        {
            var current = edge.ControlPoints[i];
            var next = i + 1 < edge.ControlPoints.Length ? edge.ControlPoints[i + 1] : new Vector3(0f, 0f, node.Position.Z);

            var mid = (current + next) / 2f;
            var dist = Vector2.Distance(localPos, mid.Xy);
            if (dist > bestDistance)
                continue;

            bestDistance = dist;
            insertIndex = i + 1;
            z = mid.Z;
        }

        localPos = GetClosestGridPoint(localPos);
        var newCp = new Vector3(localPos.X, localPos.Y, z);

        var newControlPoints = new Vector3[edge.ControlPoints.Length + 1];

        if (insertIndex > 0)
            Array.Copy(edge.ControlPoints, 0, newControlPoints, 0, insertIndex);

        newControlPoints[insertIndex] = newCp;

        if (insertIndex < edge.ControlPoints.Length)
            Array.Copy(edge.ControlPoints, insertIndex, newControlPoints, insertIndex + 1, edge.ControlPoints.Length - insertIndex);

        edge.ControlPoints = newControlPoints;
    }

    private void ControlPointHeightStep(IRacerArcadeStageRenderableEdge edge, int index, bool positive)
    {
        var adjust = positive ? GridSize : -GridSize;
        edge.ControlPoints[index].Z += adjust;
    }

    private void DeleteControlPoint(IRacerArcadeStageRenderableEdge edge, int index)
    {
        var newCp = new Vector3[edge.ControlPoints.Length - 1];

        if (index > 0)
            Array.Copy(edge.ControlPoints, 0, newCp, 0, index);

        if (index < edge.ControlPoints.Length - 1)
            Array.Copy(edge.ControlPoints, index + 1, newCp, index, edge.ControlPoints.Length - index - 1);

        edge.ControlPoints = newCp;
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

    private static (float minZ, float maxZ) GetHeightRange(RacerArcadeStageGraph graph)
    {
        var minZ = float.MaxValue;
        var maxZ = float.MinValue;

        foreach (var node in graph.Nodes.Values)
        {
            minZ = MathF.Min(minZ, node.Position.Z);
            maxZ = MathF.Max(maxZ, node.Position.Z);

            foreach (var edge in node.Connections)
            {
                if (edge is not IRacerArcadeStageRenderableEdge renderableEdge)
                    continue;

                if (!graph.TryGetNextNode(edge, out var nextNode))
                    continue;

                var points = renderableEdge.GetWorldSpaceEdgePoints(node.Position, nextNode.Position);
                var sampled = RacerArcadeStageGraphHelpers.SampleBezier(points, RenderableEdgeBezierSamples);
                for (var i = 1; i < sampled.Length; i++)
                {
                    var prev = sampled[i - 1];
                    var next = sampled[i];

                    minZ = MathF.Min(minZ, prev.Z);
                    maxZ = MathF.Max(maxZ, prev.Z);

                    minZ = MathF.Min(minZ, next.Z);
                    maxZ = MathF.Max(maxZ, next.Z);
                }
            }
        }

        return (minZ, maxZ);
    }

    private static Color MapHeightToColor(float height, float minZ, float maxZ)
    {
        if (minZ == maxZ)
            return ShadowMidTint;

        var t = (height - minZ) / (maxZ - minZ);
        t = Math.Clamp(t, 0f, 1f);

        if (t < 0.5f)
            return Color.InterpolateBetween(ShadowMidTint, ShadowLowTint, t * 2);
        else
            return Color.InterpolateBetween(ShadowMidTint, ShadowHighTint, (t - 0.5f) * 2);
    }
}
