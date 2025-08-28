using Content.Shared.PDA.Messaging.Messages;
using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class ChatUiState(Dictionary<IPdaChatRecipient, BasePdaChatMessage[]> messages) : BoundUserInterfaceState
{
    public readonly Dictionary<IPdaChatRecipient, BasePdaChatMessage[]> Messages = messages;
}
