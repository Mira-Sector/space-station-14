using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.Serialization;

namespace Content.Shared.PDA.Messaging.Messages;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ChatMessageText : IChatMessage
{
    [DataField]
    public IChatRecipient Sender { get; set; }

    [DataField]
    public IChatRecipient Recipient { get; set; }

    [DataField]
    public NetEntity Server { get; set; }

    [DataField]
    public string Contents;

    public string GetNotificationText() => Contents;
}
