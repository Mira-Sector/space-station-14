

using Robust.Shared.Map;

namespace Content.Shared.Telescience;

///<summary>
///Event raised on the teleframe and any upgrade modules it has just before teleportation occurs
/// </summary>
[ByRefEvent]
public record struct BeforeTeleportEvent(EntityUid Teleframe, bool Cancelled = false);

///<summary>
///Event raised just after on the teleframe and anything that was teleported just after teleportation
/// </summary>
[ByRefEvent]
public record struct AfterTeleportEvent(MapCoordinates To, MapCoordinates From);

///<summary>
/// Event raised on entities that are to experience a teleport incident
/// </summary>
[ByRefEvent]
public record struct TeleportIncidentEvent(float Score, float IncidentMult);

///<summary>
/// Event raised on teleframe consoles to make them speak
/// </summary>
/// [ByRefEvent]
public record struct TeleframeConsoleSpeak(string Message, bool Radio, bool Voice);
