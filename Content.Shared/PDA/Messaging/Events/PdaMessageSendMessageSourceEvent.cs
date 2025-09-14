using Content.Shared.PDA.Messaging.Messages;
using Robust.Shared.Serialization;

namespace Content.Shared.PDA.Messaging.Events;

[Serializable, NetSerializable]
public sealed partial class PdaMessageSendMessageSourceEvent(NetEntity client, BasePdaChatMessage message) : EntityEventArgs, IPdaMessagePayload
{
    public NetEntity Client { get; } = client;
    public readonly BasePdaChatMessage Message = message;
}
