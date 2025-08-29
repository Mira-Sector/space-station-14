using Content.Shared.PDA.Messaging.Messages;

namespace Content.Shared.PDA.Messaging.Events;

public sealed partial class PdaMessageReplicateAttemptClientEvent(EntityUid server, EntityUid source, BasePdaChatMessage message) : CancellableEntityEventArgs
{
    public readonly EntityUid Server = server;
    public readonly EntityUid Source = source;
    public readonly BasePdaChatMessage Message = message;
}
