using Content.Shared.PDA.Messaging.Messages;
using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class ChatUiState(PdaChatRecipientProfile profile, KeyValuePair<BasePdaChatMessageable, BasePdaChatMessage[]>[] messages, Dictionary<BasePdaChatMessageable, int> unreadMessageCount, Dictionary<NetEntity, string> availableServers, NetEntity? currentServer) : BoundUserInterfaceState
{
    public readonly PdaChatRecipientProfile Profile = profile;
    public readonly KeyValuePair<BasePdaChatMessageable, BasePdaChatMessage[]>[] Messages = messages;
    public readonly Dictionary<BasePdaChatMessageable, int> UnreadMessageCount = unreadMessageCount;
    public readonly Dictionary<NetEntity, string> AvailableServers = availableServers;
    public readonly NetEntity? CurrentServer = currentServer;
}
