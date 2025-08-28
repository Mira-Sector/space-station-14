using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.Serialization;

namespace Content.Shared.PDA.Messaging.Events;

[Serializable, NetSerializable]
public sealed partial class PdaMessageRequestMessageHistoryMessage(NetEntity client, IChatRecipient recipient) : EntityEventArgs, IPdaMessagePayload
{
    public NetEntity Client { get; } = client;
    public IChatRecipient Recipient = recipient;
}
