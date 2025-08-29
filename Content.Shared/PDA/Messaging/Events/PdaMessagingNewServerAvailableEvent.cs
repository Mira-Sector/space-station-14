namespace Content.Shared.PDA.Messaging.Events;

[ByRefEvent]
public readonly record struct PdaMessageNewServerAvailableEvent(EntityUid Server, EntityUid? Station);
