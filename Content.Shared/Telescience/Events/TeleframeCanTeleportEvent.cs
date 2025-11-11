using Robust.Shared.Map;

namespace Content.Shared.Telescience.Events;
///<summary>
///Event raised on the teleframe and any upgrade modules it has just before teleportation occurs
/// </summary>
[ByRefEvent]
public struct TeleframeCanTeleportEvent(EntityUid teleframe, MapCoordinates target)
{
    public readonly EntityUid Teleframe = teleframe;
    public readonly MapCoordinates Target = target;
    public bool Cancelled = false;
}
