using System.Linq;
using System.Numerics;
using Content.Shared.PolygonRenderer;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.PolygonRenderer;

[UsedImplicitly]
public sealed partial class PolygonRendererControl : Control
{
    [ViewVariables]
    public PolygonModel[] Models = [];

    [ViewVariables]
    public Vector3 Camera = new();

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        // painters algorithm
        // should prob replace with a depth buffer
        var polygonsWithDepth = new List<(Polygon Polygon, float Depth)>();

        foreach (var model in Models)
        {
            foreach (var polygon in model.Polygons)
            {
                if (polygon.Color == null)
                    continue;

                var avgDepth = (polygon.Vertices[0].Z + polygon.Vertices[1].Z + polygon.Vertices[2].Z) / 3f;
                polygonsWithDepth.Add((polygon, avgDepth));
            }
        }

        var sortedPolygons = polygonsWithDepth.OrderByDescending(x => x.Depth).ToList();

        foreach (var (polygon, _) in sortedPolygons)
        {
            var vertices = PolygonRenderer.PolygonTo2D(polygon, Camera);

            List<Vector2> scaledVertices = [];
            foreach (var vertex in vertices)
                scaledVertices.Add(vertex * PixelSize);

            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, scaledVertices, polygon.Color!.Value);
        }
    }
}
