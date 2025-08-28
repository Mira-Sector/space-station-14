using Content.Shared.PDA.Messaging.Components;
using Content.Shared.PDA.Messaging.Messages;

namespace Content.Shared.PDA.Messaging.Events;

[ByRefEvent]
public readonly record struct PdaMessageSentMessageServerEvent(Entity<PdaMessagingClientComponent> Client, BasePdaChatMessage Message);
