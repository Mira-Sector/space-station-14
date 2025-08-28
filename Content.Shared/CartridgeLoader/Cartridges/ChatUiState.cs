using Content.Shared.PDA.Messaging.Messages;
using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class ChatUiState(Dictionary<IChatRecipient, IChatMessage[]> messages) : BoundUserInterfaceState
{
    public readonly Dictionary<IChatRecipient, IChatMessage[]> Messages = messages;
}
