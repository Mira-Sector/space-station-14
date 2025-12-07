using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Shared.PolygonRenderer;

public abstract partial class SharedFlatShadedPolygon : SharedColoredPolygon
{
    [DataField]
    public float MinBrightness = 0.2f;


    public override Color? Shade(Vector3[] cameraVertices, Matrix4 camera)
    {
        var cameraForward = Vector3.TransformNormal(-Vector3.UnitZ, camera);
        cameraForward.Normalize();

        var normal = Normal();
        var dot = Vector3.Dot(normal, cameraForward);
        dot = Math.Abs(dot); // so backfaces arent dark

        var lightingFactor = Math.Max(MinBrightness, dot);
        var finalColor = new Color(
            Color.R * lightingFactor,
            Color.G * lightingFactor,
            Color.B * lightingFactor,
            Color.A
        );

        return finalColor;
    }

    public SharedFlatShadedPolygon(Vector3[] vertices) : base(vertices)
    {
    }

    public SharedFlatShadedPolygon(Vector3[] vertices, Color color) : base(vertices, color)
    {
    }

    public SharedFlatShadedPolygon(Vector3[] vertices, Color color, float minBrightness) : base(vertices, color)
    {
        MinBrightness = minBrightness;
    }
}
