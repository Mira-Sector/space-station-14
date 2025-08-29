using Content.Shared.PDA.Messaging.Messages;

namespace Content.Shared.PDA.Messaging.Events;

[ByRefEvent]
public readonly record struct PdaMessageReplicatedMessageServerEvent(EntityUid Client, EntityUid Source, BasePdaChatMessage Message);
