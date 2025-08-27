using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class ChatUiState(IChatRecipient[] recipients) : BoundUserInterfaceState
{
    public readonly IChatRecipient[] Recipients = recipients;
}
