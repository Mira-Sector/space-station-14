using Content.Shared.Localizations;
using Content.Shared.PDA.Messaging.Messages;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.PDA.Messaging.Recipients;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class PdaChatRecipientGroup : BasePdaChatMessageable, IRobustCloneable<PdaChatRecipientGroup>
{
    [DataField]
    public string? Name;

    [DataField]
    public ProtoId<PdaChatProfilePicturePrototype>? Picture;

    [DataField]
    public HashSet<PdaChatRecipientProfile> Members = [];

    public override string Prefix() => "GRP";

    public override IEnumerable<PdaChatRecipientProfile> GetRecipients()
    {
        foreach (var member in Members)
            yield return member;
    }

    public override BasePdaChatMessageable GetRecipientMessageable(BasePdaChatMessage message) => this;

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

    public PdaChatRecipientGroup Clone()
    {
        return new PdaChatRecipientGroup(this);
    }

    public PdaChatRecipientGroup(PdaChatRecipientGroup messageable) : base(messageable)
    {
        Name = messageable.Name;
        Picture = messageable.Picture;
        Members = messageable.Members;
    }
}
