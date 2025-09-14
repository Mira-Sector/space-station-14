using Content.Shared.PDA.Messaging.Events;
using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed partial class ChatCartridgeClearUnreadMessageCountEvent(NetEntity client, BasePdaChatMessageable contact) : EntityEventArgs, IPdaMessagePayload
{
    public NetEntity Client { get; } = client;
    public readonly BasePdaChatMessageable Contact = contact;
}
