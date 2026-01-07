using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Arcade.Racer;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RacerGameStageSkyData
{
    [DataField(required: true)]
    public SpriteSpecifier Sprite;
}
