using Content.Shared.PDA.Messaging.Messages;

namespace Content.Shared.PDA.Messaging.Events;

public sealed partial class PdaMessageSendAttemptSourceEvent(EntityUid server, BasePdaChatMessage message) : CancellableEntityEventArgs
{
    public readonly EntityUid Server = server;
    public readonly BasePdaChatMessage Message = message;
}
