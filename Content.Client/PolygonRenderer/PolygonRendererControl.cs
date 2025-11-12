using Content.Shared.PolygonRenderer;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using System.Linq;
using JetBrains.Annotations;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.PolygonRenderer;

[UsedImplicitly]
public sealed partial class PolygonRendererControl : Control
{
    [ViewVariables]
    public PolygonModel[] Models = [];

    [ViewVariables]
    public Matrix4 Camera = Matrix4.Identity;

    private record struct TransformedPolygon(Polygon Polygon, Vector3[] TransformedVertices, float AvgDepth);

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

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
                    var transformed = Vector3.Transform(polygon.Vertices[i], model.ModelMatrix);
                    transformedVertices[i] = transformed;
                    avgDepth += transformed.Z;
                }
                avgDepth /= transformedVertices.Length;

                var transformedPolygon = new TransformedPolygon(polygon, transformedVertices, avgDepth);
                transformedPolygons.Add(transformedPolygon);
            }
        }

        var sortedPolygons = transformedPolygons.OrderByDescending(x => x.AvgDepth);
        foreach (var (polygon, transformedVertices, _) in sortedPolygons)
        {
            var (vertices, color) = polygon.PolygonTo2D(transformedVertices, Camera);
            if (color == null)
                continue;

            for (var i = 0; i < vertices.Length; i++)
                vertices[i] *= PixelSize;

            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, vertices, color.Value);
        }
    }
}
