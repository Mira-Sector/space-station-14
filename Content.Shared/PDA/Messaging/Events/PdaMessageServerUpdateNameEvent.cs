namespace Content.Shared.PDA.Messaging.Events;

[ByRefEvent]
public readonly record struct PdaMessageServerUpdateNameEvent(EntityUid Client, string Name);
