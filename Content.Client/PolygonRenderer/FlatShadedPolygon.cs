using System.Numerics;
using Content.Shared.PolygonRenderer;
using Robust.Client.Graphics;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.PolygonRenderer;

public sealed partial class FlatShadedPolygon : SharedFlatShadedPolygon, IClientPolygon
{
    public void Draw(DrawingHandleScreen handle, List<Vector2> vertices, Color color)
    {
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, vertices, color);
    }

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
