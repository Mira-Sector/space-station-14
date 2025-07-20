using Content.Shared.Silicons.StationAi;
using Robust.Client.Graphics;

namespace Content.Client.Silicons.StationAi;

[DataDefinition]
public sealed partial class StationAiVisionVisualsTri : SharedStationAiVisionVisualsTri, IClientStationAiVisionVisualsShape
{
    public void Draw(DrawingHandleWorld worldHandle)
    {
        worldHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, Points, Color);
    }
}
