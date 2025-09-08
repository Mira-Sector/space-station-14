using Content.Shared.PDA.Messaging.Recipients;

namespace Content.Shared.PDA.Messaging.Events;

[ByRefEvent]
public readonly record struct PdaMessageServerClientDisconnectedEvent(EntityUid Client, PdaChatRecipientProfile Profile);
