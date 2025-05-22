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
            var modelMatrix = Matrix4.Identity;
            modelMatrix *= Matrix4.Scale(model.Scale);
            modelMatrix *= Matrix4.CreateRotationX(model.Rotation.X)
                * Matrix4.CreateRotationY(model.Rotation.Y)
                * Matrix4.CreateRotationZ(model.Rotation.Z);
            modelMatrix *= Matrix4.CreateTranslation(model.Position);

            foreach (var polygon in model.Polygons)
            {
                if (polygon.Color == null)
                    continue;

                var transformedVertices = new Vector3[3];
                for (var i = 0; i < 3; i++)
                    transformedVertices[i] = Vector3.Transform(polygon.Vertices[i], modelMatrix);

                var avgDepth = (transformedVertices[0].Z + transformedVertices[1].Z + transformedVertices[2].Z) / 3f;
                polygonsWithDepth.Add((new Polygon(transformedVertices, polygon.Color), avgDepth));
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
