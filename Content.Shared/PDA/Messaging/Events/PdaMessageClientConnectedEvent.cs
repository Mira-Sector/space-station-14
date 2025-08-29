using Content.Shared.PDA.Messaging.Recipients;

namespace Content.Shared.PDA.Messaging.Events;

[ByRefEvent]
public readonly record struct PdaMessageClientConnectedEvent(EntityUid Client, PdaChatRecipientProfile Profile);
