using Content.Shared.PolygonRenderer;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using System.Linq;
using JetBrains.Annotations;
using System.Numerics;
using Vector3 = Robust.Shared.Maths.Vector3;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Content.Client.PolygonRenderer;

[UsedImplicitly]
[Virtual]
public partial class PolygonRendererControl : Control
{
    [ViewVariables]
    public List<PolygonModel> Models = [];

    [ViewVariables]
    public Matrix4 Camera = Matrix4.Identity;

    [ViewVariables]
    public Angle FOV = Angle.FromDegrees(70f);

    [ViewVariables]
    public float ClipNear = 0.1f;

    [ViewVariables]
    public float ClipFar = 2048f;

    private record struct TransformedPolygon(IClientPolygon Polygon, List<Vector2> Vertices, Color Color, float AvgDepth);

    [MustCallBase]
    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var screenCenter = PixelSize * 0.5f;
        var aspect = PixelSize.X / PixelSize.Y;
        var projection = Matrix4.CreatePerspectiveFieldOfView((float)FOV.Theta, aspect, ClipNear, ClipFar);

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
                var cameraVertices = new Vector3[polygon.Vertices.Length];
                var projectedVertices = new Vector2[polygon.Vertices.Length];
                for (var i = 0; i < polygon.Vertices.Length; i++)
                {
                    var worldPos = Vector3.Transform(polygon.Vertices[i], model.ModelMatrix);
                    var worldPos4 = new Vector4(worldPos, 1f);

                    var viewPos = Vector4.Transform(worldPos4, Camera);
                    cameraVertices[i] = viewPos.Xyz;

                    var clip = Vector4.Transform(viewPos, projection);

                    clip /= clip.W;

                    var screenPos = new Vector2(
                        (clip.X * 0.5f + 0.5f) * PixelSize.X,
                        (-clip.Y * 0.5f + 0.5f) * PixelSize.Y
                    );

                    projectedVertices[i] = screenPos;
                    avgDepth += clip.Z;
                }

                var color = polygon.Shade(cameraVertices, Camera);
                if (color == null)
                    continue;

                avgDepth /= polygon.Vertices.Length;

                var transformedPolygon = new TransformedPolygon((IClientPolygon)polygon, projectedVertices.ToList(), color.Value, avgDepth);
                transformedPolygons.Add(transformedPolygon);
            }
        }

        var sortedPolygons = transformedPolygons.OrderByDescending(x => x.AvgDepth);
        foreach (var (polygon, vertices, color, _) in sortedPolygons)
            polygon.Draw(handle, vertices, color);
    }
}
