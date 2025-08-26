using System.Linq;
using Content.Shared.Localizations;
using Robust.Shared.Serialization;

namespace Content.Shared.PDA.Messaging.Recipients;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ChatRecipientGroup : IChatRecipient
{
    [DataField]
    public HashSet<ChatRecipientProfile> Members = [];

    public string GetNotificationText()
    {
        return ContentLocalizationManager.FormatList(Members.Select(x => x.Name).ToList());
    }
}
