using Content.Shared.PDA.Messaging.Events;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class ChatUiMessageEvent(IPdaMessagePayload payload) : CartridgeMessageEvent
{
    public readonly IPdaMessagePayload Payload = payload;
}
