using Content.Shared.PolygonRenderer;
using Robust.Client.Graphics;
using System.Numerics;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.PolygonRenderer;

public sealed partial class ColoredPolygon : SharedColoredPolygon, IClientPolygon
{
    public void Draw(DrawingHandleScreen handle, List<Vector2> vertices, Color color)
    {
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, vertices, color);
    }

    public ColoredPolygon(Vector3[] vertices) : base(vertices)
    {
    }

    public ColoredPolygon(Vector3[] vertices, Color color) : base(vertices, color)
    {
    }
}
