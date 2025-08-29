using Content.Shared.PDA.Messaging.Recipients;

namespace Content.Shared.PDA.Messaging.Events;

[ByRefEvent]
public readonly record struct PdaMessageClientDisconnectedEvent(EntityUid Client, PdaChatRecipientProfile Profile);
