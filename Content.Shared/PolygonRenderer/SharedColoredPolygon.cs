using System.Numerics;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Shared.PolygonRenderer;

public abstract partial class SharedColoredPolygon : BasePolygon
{
    [DataField]
    public Color Color = Color.White;

    public override (Vector2[], Color?) PolygonTo2D(Vector3[] cameraVertices, Matrix4 camera)
    {
        var (vertices, _) = base.PolygonTo2D(cameraVertices, camera);
        return (vertices, Color);
    }

    public SharedColoredPolygon(Vector3[] vertices) : base(vertices)
    {
    }

    public SharedColoredPolygon(Vector3[] vertices, Color color) : base(vertices)
    {
        Color = color;
    }
}
