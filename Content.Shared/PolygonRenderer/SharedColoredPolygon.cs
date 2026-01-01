using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Shared.PolygonRenderer;

public abstract partial class SharedColoredPolygon : BasePolygon
{
    [DataField]
    public Color Color = Color.White;

    public override Color? Shade(Vector3[] cameraVertices, Matrix4 camera)
    {
        return Color;
    }

    public SharedColoredPolygon(Vector3[] vertices) : base(vertices)
    {
    }

    public SharedColoredPolygon(Vector3[] vertices, Color color) : base(vertices)
    {
        Color = color;
    }
}
