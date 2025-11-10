using Content.Shared.Arcade.Racer;
using Content.Shared.Arcade.Racer.Stage;
using Robust.Client.Graphics;
using System.Numerics;

namespace Content.Client.Arcade.Racer;

public sealed partial class RacerEditorViewportControl
{
    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_data is not { } data)
            return;

        DrawSky(handle, data.Sky);
        DrawGrid(handle);
        DrawGraph(handle, data.Graph);
        DrawEdgeControlPoints(handle, data.Graph);

        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawSky(DrawingHandleScreen handle, RacerGameStageSkyData data)
    {
        handle.SetTransform(Matrix3x2.Identity);
        var texture = _sprite.Frame0(data.Sprite);
        handle.DrawTextureRect(texture, PixelSizeBox);
    }

    private void DrawGrid(DrawingHandleScreen handle)
    {
        handle.SetTransform(Matrix3x2.Identity);

        handle.DrawRect(PixelSizeBox, GridBackgroundColor);

        var topLeft = Vector2.Transform(Vector2.Zero, InverseTransform);
        var bottomRight = Vector2.Transform(PixelSize, InverseTransform);

        var gridStart = new Vector2(
            MathF.Floor(topLeft.X / GridSize) * GridSize,
            MathF.Floor(topLeft.Y / GridSize) * GridSize
        );

        var gridEnd = new Vector2(
            MathF.Floor(bottomRight.X / GridSize) * GridSize,
            MathF.Floor(bottomRight.Y / GridSize) * GridSize
        );

        for (var x = gridStart.X; x <= gridEnd.X; x += GridSize)
        {
            var from = new Vector2(x, topLeft.Y);
            var to = new Vector2(x, bottomRight.Y);
            var color = x / GridSize % 8 == 0 ? Mul8GridColor : GridColor;
            DrawGridLine(handle, from, to, color);
        }

        for (var y = gridStart.Y; y <= gridEnd.Y; y += GridSize)
        {
            var from = new Vector2(topLeft.X, y);
            var to = new Vector2(bottomRight.X, y);
            var color = y / GridSize % 8 == 0 ? Mul8GridColor : GridColor;
            DrawGridLine(handle, from, to, color);
        }

        {
            var from = new Vector2(topLeft.X, 0);
            var to = new Vector2(bottomRight.X, 0);
            DrawGridLine(handle, from, to, OriginGridColor);
        }

        {
            var from = new Vector2(0, topLeft.Y);
            var to = new Vector2(0, bottomRight.Y);
            DrawGridLine(handle, from, to, OriginGridColor);
        }
    }

    private void DrawGridLine(DrawingHandleScreen handle, Vector2 from, Vector2 to, Color color)
    {
        from = Vector2.Transform(from, Transform);
        to = Vector2.Transform(to, Transform);

        handle.DrawLine(from, to, OriginGridColor);
    }

    private void DrawGraph(DrawingHandleScreen handle, RacerArcadeStageGraph graph)
    {
        // draw edges first
        foreach (var (edge, node) in graph.GetConnections())
        {
            // not traversing to other graphs as we only edit a single graph
            if (!graph.TryGetNextNode(edge, out var nextNode))
                continue;

            if (edge is IRacerArcadeStageRenderableEdge renderableEdge)
                DrawRenderableEdge(handle, renderableEdge, node.Position, nextNode.Position);
            else
                DrawStandardEdgeEdge(handle, edge, node.Position, nextNode.Position);
        }

        foreach (var node in graph.Nodes.Values)
            DrawNode(handle, node);
    }

    private void DrawNode(DrawingHandleScreen handle, RacerArcadeStageNode node)
    {
        handle.SetTransform(Transform);

        if (_selectedNode == node)
            handle.DrawCircle(node.Position, NodeRadius, SelectedNodeColor);
        else
            handle.DrawCircle(node.Position, NodeRadius, NodeColor);
    }

    private void DrawRenderableEdge(DrawingHandleScreen handle, IRacerArcadeStageRenderableEdge renderableEdge, Vector2 sourcePos, Vector2 nextPos)
    {
        if (!_prototype.TryIndex(renderableEdge.Texture, out var texture))
        {
            DrawStandardEdgeEdge(handle, renderableEdge, sourcePos, nextPos);
            return;
        }
        var edgeTexture = _sprite.Frame0(texture.Texture);

        var pixelToWorldScale = renderableEdge.Width / edgeTexture.Width;
        var textureWorldHeight = edgeTexture.Height * pixelToWorldScale;

        var halfWidth = renderableEdge.Width * 0.5f;

        var points = GetWorldSpaceEdgePoints(renderableEdge, sourcePos, nextPos);
        var sampled = SampleBezier(points, RenderableEdgeBezierSamples);

        var color = _selectedEdge == renderableEdge ? SelectedEdgeColor : Color.White;

        var totalLength = 0f;
        var distances = new float[sampled.Length];
        for (var i = 1; i < sampled.Length; i++)
        {
            var start = sampled[i - 1];
            var end = sampled[i];

            totalLength += Vector2.Distance(start, end);
            distances[i] = totalLength;
        }

        var currentDistance = 0f;
        while (currentDistance < totalLength)
        {
            var segIndex = 1;
            while (segIndex < distances.Length && distances[segIndex] < currentDistance)
                segIndex++;

            if (segIndex >= distances.Length)
                break;

            var prev = sampled[segIndex - 1];
            var next = sampled[segIndex];
            var segLen = Vector2.Distance(prev, next);

            var segStartDist = distances[segIndex - 1];
            var t = (currentDistance - segStartDist) / segLen;
            t = Math.Clamp(t, 0f, 1f);

            var pos = Vector2.Lerp(prev, next, t);
            var dir = (next - prev).Normalized();
            var angle = MathF.Atan2(dir.Y, dir.X) - MathHelper.PiOver2;

            var rect = new UIBox2(
                -halfWidth, 0f,
                halfWidth, textureWorldHeight
            );

            var matty = Matrix3x2.CreateRotation(angle) * Matrix3x2.CreateTranslation(pos) * Transform;
            handle.SetTransform(matty);
            handle.DrawTextureRect(edgeTexture, rect, color);

            currentDistance += textureWorldHeight;
        }
    }

    private void DrawStandardEdgeEdge(DrawingHandleScreen handle, IRacerArcadeStageEdge edge, Vector2 sourcePos, Vector2 nextPos)
    {
        handle.SetTransform(Transform);

        if (_selectedEdge == edge)
            handle.DrawLine(sourcePos, nextPos, SelectedEdgeColor);
        else
            handle.DrawLine(sourcePos, nextPos, StandardEdgeColor);
    }

    private void DrawEdgeControlPoints(DrawingHandleScreen handle, RacerArcadeStageGraph graph)
    {
        if (_selectedEdge is not IRacerArcadeStageRenderableEdge edge)
            return;

        if (!graph.TryGetParentNode(edge, out var node))
            return;

        handle.SetTransform(Transform);
        foreach (var cp in edge.ControlPoints)
        {
            var point = cp.Xy + node.Position;
            handle.DrawCircle(point, ControlPointRadius, ControlPointColor);
        }
    }
}
