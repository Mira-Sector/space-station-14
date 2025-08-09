using Content.Shared.Silicons.StationAi;
using Robust.Client.Graphics;

namespace Content.Client.Silicons.StationAi;

[DataDefinition]
public sealed partial class StationAiVisionVisualsRect : SharedStationAiVisionVisualsRect, IClientStationAiVisionVisualsShape
{
    public void Draw(DrawingHandleWorld worldHandle)
    {
        worldHandle.DrawRect(Rect, Color);
    }
}
