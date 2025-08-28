using Content.Shared.PDA.Messaging.Messages;
using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class ChatUiState(PdaChatRecipientProfile profile, Dictionary<BasePdaChatMessageable, BasePdaChatMessage[]> messages) : BoundUserInterfaceState
{
    public readonly PdaChatRecipientProfile Profile = profile;
    public readonly Dictionary<BasePdaChatMessageable, BasePdaChatMessage[]> Messages = messages;
}
