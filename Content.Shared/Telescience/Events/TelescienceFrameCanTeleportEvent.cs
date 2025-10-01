namespace Content.Shared.Telescience.Events;

///<summary>
///Event raised on the teleframe and any upgrade modules it has just before teleportation occurs
/// </summary>
[ByRefEvent]
public struct TelescienceFrameCanTeleportEvent(EntityUid teleframe)
{
    public readonly EntityUid Teleframe = teleframe;
    public bool Cancelled = false;
}
