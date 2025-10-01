using Robust.Shared.Map;

namespace Content.Shared.Telescience.Events;

///<summary>
///Event raised just after on the teleframe just after teleportation an entity
/// </summary>
[ByRefEvent]
public readonly record struct TelescienceFrameTeleportedEvent(EntityUid Teleported, MapCoordinates To, MapCoordinates From);
