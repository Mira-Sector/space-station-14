using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class ChatUiState(HashSet<IChatRecipient> recipients) : BoundUserInterfaceState
{
    public readonly HashSet<IChatRecipient> Recipients = recipients;
}
