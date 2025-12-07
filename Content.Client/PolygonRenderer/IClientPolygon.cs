using System.Numerics;
using Robust.Client.Graphics;

namespace Content.Client.PolygonRenderer;

public interface IClientPolygon
{
    void Draw(DrawingHandleScreen handle, List<Vector2> vertices, Color color);
}
