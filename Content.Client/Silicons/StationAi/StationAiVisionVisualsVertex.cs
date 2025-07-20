using Content.Shared.Silicons.StationAi;
using Robust.Client.Graphics;

namespace Content.Client.Silicons.StationAi;

[DataDefinition]
public sealed partial class StationAiVisionVisualsVertex : SharedStationAiVisionVisualsVertex, IClientStationAiVisionVisualsShape
{
    public void Draw(DrawingHandleWorld worldHandle)
    {
        worldHandle.DrawLine(Start, End, Color);
    }
}
