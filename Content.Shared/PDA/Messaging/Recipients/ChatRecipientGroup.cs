using System.Linq;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.PDA.Messaging.Recipients;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ChatRecipientGroup : IChatRecipient
{
    [DataField]
    public string? Name;

    [DataField]
    public ProtoId<ChatProfilePicturePrototype>? Picture;

    [DataField]
    public HashSet<ChatRecipientProfile> Members = [];

    public SpriteSpecifier GetUiIcon(IPrototypeManager prototype)
    {
        if (Picture is { } picture)
        {
            var pfp = prototype.Index(picture);
            return pfp.Sprite;
        }

        var member = Members.First();
        return member.GetUiIcon(prototype);
    }

    public string GetUiName()
    {
        return Name ?? GetGenericName();
    }

    public string GetNotificationText()
    {
        return Name ?? GetGenericName();
    }

    private string GetGenericName()
    {
        return ContentLocalizationManager.FormatList(Members.Select(x => x.Name).ToList());
    }
}
