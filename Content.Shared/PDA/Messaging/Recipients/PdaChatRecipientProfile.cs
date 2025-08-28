using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.PDA.Messaging.Recipients;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class PdaChatRecipientProfile : IPdaChatRecipient
{
    [DataField]
    public string Name;

    [DataField]
    public ProtoId<PdaChatProfilePicturePrototype> Picture;

    public SpriteSpecifier GetUiIcon(IPrototypeManager prototype)
    {
        var pfp = prototype.Index(Picture);
        return pfp.Sprite;
    }

    public string GetUiName() => Name;
    public string GetNotificationText() => Name;
}
