namespace Content.Shared.PDA.Messaging.Events;

[ByRefEvent]
public readonly record struct PdaMessageServerRemovedEvent(EntityUid Server, EntityUid? Station);
