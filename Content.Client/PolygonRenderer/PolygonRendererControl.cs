using Content.Shared.PolygonRenderer;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client.PolygonRenderer;

[UsedImplicitly]
public sealed partial class PolygonRendererControl : Control
{
    public List<PolygonModel> Models = [];
    public Vector3 Camera = new();

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        foreach (var model in Models)
        {
            foreach (var polygon in model.Polygons)
            {
                if (polygon.Color == null)
                    return;

                var vertices = PolygonRenderer.PolygonTo2D(polygon, Camera);
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, vertices, polygon.Color.Value);
            }
        }
    }
}
