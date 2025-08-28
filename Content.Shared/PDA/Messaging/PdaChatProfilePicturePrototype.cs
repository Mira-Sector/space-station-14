using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.PDA.Messaging;

[Prototype]
public sealed partial class PdaChatProfilePicturePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public SpriteSpecifier Sprite = default!;
}
