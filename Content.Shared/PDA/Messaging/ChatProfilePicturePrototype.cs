using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.PDA.Messaging;

[Prototype]
public sealed partial class ChatProfilePicturePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public SpriteSpecifier Sprite = default!;
}
