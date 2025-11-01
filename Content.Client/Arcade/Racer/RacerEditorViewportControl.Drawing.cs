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
        DrawGraph(handle, data.Graph);

        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawSky(DrawingHandleScreen handle, RacerGameStageSkyData data)
    {
        handle.SetTransform(Matrix3x2.Identity);
        var texture = _sprite.Frame0(data.Sprite);
        handle.DrawTextureRect(texture, PixelSizeBox);
    }

    private void DrawGraph(DrawingHandleScreen handle, RacerArcadeStageGraph graph)
    {
        // draw edges first
        foreach (var node in graph.Nodes.Values)
        {
            foreach (var edge in node.Connections)
            {
                // not traversing to other graphs as we only edit a single graph
                if (!graph.TryGetNextNode(edge, out var nextNode))
                    continue;

                if (edge is IRacerArcadeStageRenderableEdge renderableEdge)
                    DrawRenderableEdge(handle, renderableEdge, node.Position, nextNode.Position);
                else
                    DrawStandardEdgeEdge(handle, edge, node.Position, nextNode.Position);
            }
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
        // convert from relative to world positions
        List<Vector2> points = new(renderableEdge.ControlPoints.Count);
        foreach (var cp in renderableEdge.ControlPoints)
            points.Add(cp + sourcePos);
        points.Add(nextPos);

        var sampled = SampleBezier(points, RenderableEdgeBezierSamples);

        var texture = _sprite.Frame0(renderableEdge.Texture);
        for (var i = 1; i < sampled.Count; i++)
        {
            var start = sampled[i - 1];
            var end = sampled[i];

            var segment = end - start;
            var angle = MathF.Atan2(segment.Y, segment.X);
            var rect = new UIBox2(start, end);

            var matty = Transform * Matrix3x2.CreateRotation(angle, start);
            handle.SetTransform(matty);
            handle.DrawTextureRect(texture, rect);

            handle.SetTransform(Matrix3x2.Identity);
            handle.DrawLine(start, end, StandardEdgeColor);
        }
    }

    private void DrawStandardEdgeEdge(DrawingHandleScreen handle, IRacerArcadeStageEdge edge, Vector2 sourcePos, Vector2 nextPos)
    {
        handle.SetTransform(Transform);
        handle.DrawLine(sourcePos, nextPos, StandardEdgeColor);
    }

}
