using Content.Shared.PDA.Messaging.Messages;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.PDA.Messaging.Recipients;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class PdaChatRecipientProfile : BasePdaChatMessageable
{
    [DataField]
    public string Name;

    [DataField]
    public ProtoId<PdaChatProfilePicturePrototype> Picture;

    public override string Prefix() => "CLT";

    public override IEnumerable<PdaChatRecipientProfile> GetRecipients()
    {
        yield return this;
    }

    public override BasePdaChatMessageable GetRecipientMessageable(BasePdaChatMessage message) => message.Sender;

    public override SpriteSpecifier GetUiIcon(IPrototypeManager prototype)
    {
        var pfp = prototype.Index(Picture);
        return pfp.Sprite;
    }

    public override string GetUiName() => Name;

    public override string GetNotificationText()
    {
        var name = Loc.GetString(Name);
        var id = Loc.GetString("pda-messaging-contact-id-wrapper", ("id", Id));
        return $"{name} {id}";
    }
}
