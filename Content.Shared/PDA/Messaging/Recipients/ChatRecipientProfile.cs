using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.PDA.Messaging.Recipients;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ChatRecipientProfile : IChatRecipient
{
    [DataField]
    public string Name;

    [DataField]
    public ProtoId<ChatProfilePicturePrototype> Picture;

    public string GetNotificationText() => Name;
}
