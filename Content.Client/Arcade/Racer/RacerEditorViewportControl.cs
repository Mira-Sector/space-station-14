using Content.Shared.Arcade.Racer;
using Content.Shared.Arcade.Racer.Stage;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using System.Linq;
using System.Numerics;

namespace Content.Client.Arcade.Racer;

public sealed partial class RacerEditorViewportControl : Control
{
    [Dependency] private readonly IEntityManager _entity = default!;
    private readonly SpriteSystem _sprite;

    private RacerGameStageEditorData? _data = null;

    private const float NodeRadius = 4f;
    private static readonly Color NodeColor = Color.Yellow;

    private static readonly Color StandardEdgeColor = Color.Green;
    private const int RenderableEdgeBezierSamples = 32;

    public RacerEditorViewportControl() : base()
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entity.System<SpriteSystem>();
    }

    public void SetData(RacerGameStageEditorData data)
    {
        _data = data;
    }

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

    private static void DrawNode(DrawingHandleScreen handle, RacerArcadeStageNode node)
    {
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

            var matty = Matrix3x2.CreateRotation(angle, start);
            handle.SetTransform(matty);
            handle.DrawTextureRect(texture, rect);

            handle.SetTransform(Matrix3x2.Identity);
            handle.DrawLine(start, end, StandardEdgeColor);
        }
    }

    private static void DrawStandardEdgeEdge(DrawingHandleScreen handle, IRacerArcadeStageEdge edge, Vector2 sourcePos, Vector2 nextPos)
    {
        handle.DrawLine(sourcePos, nextPos, StandardEdgeColor);
    }

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
}
