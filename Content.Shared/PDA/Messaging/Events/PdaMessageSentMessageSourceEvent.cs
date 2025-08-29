using Content.Shared.PDA.Messaging.Messages;

namespace Content.Shared.PDA.Messaging.Events;

[ByRefEvent]
public readonly record struct PdaMessageSentMessageSourceEvent(EntityUid Server, BasePdaChatMessage Message);
