using Content.Shared.PDA.Messaging.Recipients;

namespace Content.Shared.PDA.Messaging.Events;

[ByRefEvent]
public readonly record struct PdaMessageNewProfileServerEvent(EntityUid Server, PdaChatRecipientProfile Profile, EntityUid? Client);
