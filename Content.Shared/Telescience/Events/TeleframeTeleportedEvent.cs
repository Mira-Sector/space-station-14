using Robust.Shared.Map;

namespace Content.Shared.Telescience.Events;

///<summary>
///Event raised just after teleportation of an entity
/// </summary>
[ByRefEvent]
public readonly record struct TeleframeTeleportedEvent(EntityUid Teleported, MapCoordinates To, MapCoordinates From);
