using Content.Shared.PolygonRenderer;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using System.Linq;
using JetBrains.Annotations;
using System.Numerics;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.PolygonRenderer;

[UsedImplicitly]
[Virtual]
public partial class PolygonRendererControl : Control
{
    [ViewVariables]
    public List<PolygonModel> Models = [];

    [ViewVariables]
    public Matrix4 Camera = Matrix4.Identity;

    private record struct TransformedPolygon(List<Vector2> Vertices, Color Color, float AvgDepth);

    [MustCallBase]
    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var screenCenter = PixelSize * 0.5f;

        // painters algorithm
        // should prob replace with a depth buffer
        List<TransformedPolygon> transformedPolygons = [];
        foreach (var model in Models)
        {
            transformedPolygons.EnsureCapacity(transformedPolygons.Count + model.Polygons.Count);
            foreach (var polygon in model.Polygons)
            {
                // prevent division by 0
                // this is only a rough guide anyway
                var avgDepth = float.Epsilon;
                var transformedVertices = new Vector3[polygon.Vertices.Length];
                for (var i = 0; i < transformedVertices.Length; i++)
                {
                    var worldPos = Vector3.Transform(polygon.Vertices[i], model.ModelMatrix);
                    var camPos = Vector3.Transform(worldPos, Camera);
                    transformedVertices[i] = camPos;
                    avgDepth += camPos.Z;
                }

                var (vertices2d, color) = polygon.PolygonTo2D(transformedVertices, Camera);
                if (color == null)
                    continue;

                for (var i = 0; i < vertices2d.Length; i++)
                {
                    vertices2d[i] *= PixelSize;
                    vertices2d[i] += screenCenter;
                }

                avgDepth /= transformedVertices.Length;

                var transformedPolygon = new TransformedPolygon(vertices2d.ToList(), color.Value, avgDepth);
                transformedPolygons.Add(transformedPolygon);
            }
        }

        var sortedPolygons = transformedPolygons.OrderByDescending(x => x.AvgDepth);
        foreach (var (vertices, color, _) in sortedPolygons)
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, vertices, color);
    }
}
