using Content.Shared.PolygonRenderer;

namespace Content.Server.PolygonRenderer;

public sealed partial class ColoredPolygon : SharedColoredPolygon
{
    public ColoredPolygon(Vector3[] vertices) : base(vertices)
    {
    }

    public ColoredPolygon(Vector3[] vertices, Color color) : base(vertices, color)
    {
    }
}
