using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Body.Prototypes;

[Prototype]
public sealed partial class OrganPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public SpriteSpecifier EmptySlotSprite = default!;
}
