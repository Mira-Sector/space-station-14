using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.MedicalScanner;

[RegisterComponent]
public sealed partial class HealthAnalyzerBodyItemComponent : Component
{
    [DataField]
    public HealthAnalyzerBodyItemSprites? Sprites;

    [DataField(required: true)]
    public HealthAnalyzerBodyItemBarPosition ProgressBarLocation = default!;
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class HealthAnalyzerBodyItemSprites
{
    [DataField(required: true)]
    public SpriteSpecifier HoverSprite = default!;

    [DataField(required: true)]
    public SpriteSpecifier SelectedSprite = default!;
}
