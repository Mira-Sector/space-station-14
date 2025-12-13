using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Arcade.Racer;

[Prototype]
public sealed partial class RacerGameEdgeTexturePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SpriteSpecifier Texture = default!;
}
