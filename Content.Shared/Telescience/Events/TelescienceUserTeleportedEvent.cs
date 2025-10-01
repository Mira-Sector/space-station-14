using Robust.Shared.Map;

namespace Content.Shared.Telescience.Events;

///<summary>
///Event raised just after on the user of a teleframe just after teleportation
/// </summary>
[ByRefEvent]
public readonly record struct TelescienceUserTeleportedEvent(EntityUid Teleframe, MapCoordinates To, MapCoordinates From);
