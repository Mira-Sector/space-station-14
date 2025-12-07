using Content.Shared.PolygonRenderer;

namespace Content.Server.PolygonRenderer;

public sealed partial class FlatShadedPolygon : SharedFlatShadedPolygon
{
    public FlatShadedPolygon(Vector3[] vertices) : base(vertices)
    {
    }

    public FlatShadedPolygon(Vector3[] vertices, Color color) : base(vertices, color)
    {
    }

    public FlatShadedPolygon(Vector3[] vertices, Color color, float minBrightness) : base(vertices, color, minBrightness)
    {
    }
}
