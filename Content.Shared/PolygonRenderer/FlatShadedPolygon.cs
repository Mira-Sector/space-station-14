using System.Numerics;
using Robust.Shared.Serialization;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Shared.PolygonRenderer;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class FlatShadedPolygon : ColoredPolygon
{
    [DataField]
    public float MinBrightness = 0.2f;

    public override (List<Vector2>, Color?) PolygonTo2D(Vector3 camera)
    {
        var (vertices, _) = base.PolygonTo2D(camera);

        Vector3 cameraNormal = new(camera);
        cameraNormal.Normalize();

        var normal = Normal();
        var dot = Vector3.Dot(normal, cameraNormal);

        var lightingFactor = Math.Max(MinBrightness, dot);
        var finalColor = new Color(
            Color.R * lightingFactor,
            Color.G * lightingFactor,
            Color.B * lightingFactor
        );

        return (vertices, finalColor);
    }

    public FlatShadedPolygon(Vector3[] vertices) : base(vertices)
    {
    }

    public FlatShadedPolygon(Vector3[] vertices, Color color) : base(vertices, color)
    {
    }
}
