using Content.Shared.PDA.Messaging.Messages;

namespace Content.Shared.PDA.Messaging.Events;

[ByRefEvent]
public readonly record struct PdaMessageReplicatedMessageClientEvent(EntityUid Server, EntityUid Source, BasePdaChatMessage Message);
