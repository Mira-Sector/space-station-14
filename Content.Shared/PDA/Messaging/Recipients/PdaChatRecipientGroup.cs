using System.Linq;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.PDA.Messaging.Recipients;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class PdaChatRecipientGroup : BasePdaChatMessageable
{
    [DataField]
    public string? Name;

    [DataField]
    public ProtoId<PdaChatProfilePicturePrototype>? Picture;

    [DataField]
    public HashSet<PdaChatRecipientProfile> Members = [];

    public override string Prefix() => "GRP";

    public override SpriteSpecifier GetUiIcon(IPrototypeManager prototype)
    {
        if (Picture is { } picture)
        {
            var pfp = prototype.Index(picture);
            return pfp.Sprite;
        }

        var member = Members.First();
        return member.GetUiIcon(prototype);
    }

    public override string GetUiName()
    {
        return Name ?? GetGenericName();
    }

    public override string GetNotificationText()
    {
        return Name ?? GetGenericName();
    }

    private string GetGenericName()
    {
        return ContentLocalizationManager.FormatList(Members.Select(x => x.Name).ToList());
    }
}
