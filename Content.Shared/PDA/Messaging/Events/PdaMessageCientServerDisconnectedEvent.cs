namespace Content.Shared.PDA.Messaging.Events;

[ByRefEvent]
public readonly record struct PdaMessageClientServerDisconnectedEvent(EntityUid Server, bool Transferred);
