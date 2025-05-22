using System.Numerics;
using Robust.Shared.Serialization;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Shared.PolygonRenderer;

[DataDefinition, Virtual, Serializable, NetSerializable]
public partial class ColoredPolygon : Polygon
{
    [DataField]
    public Color Color = Color.White;

    public override (List<Vector2>, Color?) PolygonTo2D(Vector3 camera)
    {
        var (vertices, _) = base.PolygonTo2D(camera);
        return (vertices, Color);
    }

    public ColoredPolygon(Vector3[] vertices) : base(vertices)
    {
    }

    public ColoredPolygon(Vector3[] vertices, Color color) : base(vertices)
    {
        Color = color;
    }
}
