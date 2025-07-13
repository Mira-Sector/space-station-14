using Content.Shared.Silicons.StationAi;
using Robust.Client.Graphics;
using System.Numerics;

namespace Content.Client.Silicons.StationAi;

[DataDefinition]
public sealed partial class StationAiVisionVisualsRect : SharedStationAiVisionVisualsRect, IClientStationAiVisionVisualsShape
{
    public void Draw(DrawingHandleWorld worldHandle, Vector2 pos, Angle rot)
    {
        var rotated = new Box2Rotated(Rect, rot);
        worldHandle.DrawRect(rotated, Color);
    }
}
