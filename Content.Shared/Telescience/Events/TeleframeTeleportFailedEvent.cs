namespace Content.Shared.Telescience.Events;

///<summary>
///Event raised on the teleframe when failing to teleport
/// </summary>
[ByRefEvent]
public readonly record struct TeleframeTeleportFailedEvent(string Reason);
