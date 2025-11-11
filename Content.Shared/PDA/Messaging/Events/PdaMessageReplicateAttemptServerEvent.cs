using Content.Shared.PDA.Messaging.Messages;

namespace Content.Shared.PDA.Messaging.Events;

public sealed partial class PdaMessageReplicateAttemptServerEvent(EntityUid client, EntityUid source, BasePdaChatMessage message) : CancellableEntityEventArgs
{
    public readonly EntityUid Client = client;
    public readonly EntityUid Source = source;
    public readonly BasePdaChatMessage Message = message;
}
