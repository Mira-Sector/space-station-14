using Content.Shared.PDA.Messaging.Components;
using Content.Shared.PDA.Messaging.Messages;

namespace Content.Shared.PDA.Messaging.Events;

public sealed partial class PdaMessageSendAttemptServerEvent(Entity<PdaMessagingClientComponent> client, BasePdaChatMessage message) : CancellableEntityEventArgs
{
    public readonly Entity<PdaMessagingClientComponent> Client = client;
    public readonly BasePdaChatMessage Message = message;
}
