using Content.Shared.Silicons.StationAi;
using Robust.Client.Graphics;
using System.Numerics;

namespace Content.Client.Silicons.StationAi;

[DataDefinition]
public sealed partial class StationAiVisionVisualsVertex : SharedStationAiVisionVisualsVertex, IClientStationAiVisionVisualsShape
{
    public void Draw(DrawingHandleWorld worldHandle, Vector2 pos, Angle rot)
    {
        var from = Start;
        var to = End;
        rot.RotateVec(from);
        rot.RotateVec(to);
        from += pos;
        to += pos;
        worldHandle.DrawLine(from, to, Color);
    }
}
