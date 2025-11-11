using Content.Shared.PolygonRenderer;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using System.Linq;
using System.Numerics;
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

    private record TransformedPolygon(Polygon Polygon, Vector3[] TransformedVertices, float AvgDepth);

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        // painters algorithm
        // should prob replace with a depth buffer
        List<TransformedPolygon> transformedPolygons = [];

        foreach (var model in Models)
        {
            var modelMatrix = Matrix4.Identity;
            modelMatrix *= Matrix4.Scale(model.Scale);
            modelMatrix *= Matrix4.CreateRotationX(model.Rotation.X)
                * Matrix4.CreateRotationY(model.Rotation.Y)
                * Matrix4.CreateRotationZ(model.Rotation.Z);
            modelMatrix *= Matrix4.CreateTranslation(model.Position);

            transformedPolygons.EnsureCapacity(transformedPolygons.Count + model.Polygons.Count);
            foreach (var polygon in model.Polygons)
            {
                var transformedVertices = new Vector3[3];
                for (var i = 0; i < 3; i++)
                    transformedVertices[i] = Vector3.Transform(polygon.Vertices[i], modelMatrix);

                polygon.Vertices = transformedVertices;

                var avgDepth = (transformedVertices[0].Z + transformedVertices[1].Z + transformedVertices[2].Z) / 3f;
                var transformed = new TransformedPolygon(polygon, transformedVertices, avgDepth);
                transformedPolygons.Add(transformed);
            }
        }

        var sortedPolygons = transformedPolygons.OrderByDescending(x => x.AvgDepth);
        foreach (var (polygon, transformedVertices, _) in sortedPolygons)
        {
            polygon.Vertices = transformedVertices;
            var (vertices, color) = polygon.PolygonTo2D(Camera);

            List<Vector2> scaledVertices = [];
            foreach (var vertex in vertices)
                scaledVertices.Add(vertex * PixelSize);

            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, scaledVertices, color!.Value);
        }
    }
}
