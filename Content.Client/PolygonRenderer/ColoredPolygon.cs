using System.Numerics;
using Content.Shared.PolygonRenderer;
using Robust.Client.Graphics;

namespace Content.Client.PolygonRenderer;

public sealed partial class ColoredPolygon : SharedColoredPolygon, IClientPolygon
{
    public void Draw(DrawingHandleScreen handle, List<Vector2> vertices, Color color)
    {
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, vertices, color);
    }
}
