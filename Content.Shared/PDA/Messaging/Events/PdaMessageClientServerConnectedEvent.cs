namespace Content.Shared.PDA.Messaging.Events;

[ByRefEvent]
public readonly record struct PdaMessageClientServerConnectedEvent(EntityUid Server, bool Transferred);
