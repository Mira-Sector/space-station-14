using Robust.Shared.Map;

namespace Content.Server.Supermatter.Events;

public sealed partial class SupermatterDelaminationTeleportGetPositionEvent : HandledEntityEventArgs
{
    public Dictionary<EntityUid, MapCoordinates> Entities;

    public SupermatterDelaminationTeleportGetPositionEvent(Dictionary<EntityUid, MapCoordinates> entities)
    {
        Entities = entities;
    }
}
