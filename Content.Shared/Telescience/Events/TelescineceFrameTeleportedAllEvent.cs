using Robust.Shared.Map;

namespace Content.Shared.Telescience.Events;

///<summary>
///Event raised just after on the teleframe just after teleporting every possible entity
/// </summary>
[ByRefEvent]
public readonly record struct TelescienceFrameTeleportedAllEvent(List<EntityUid> Teleported, MapCoordinates To, MapCoordinates From);
