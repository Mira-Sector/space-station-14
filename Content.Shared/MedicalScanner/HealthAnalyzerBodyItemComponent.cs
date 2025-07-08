using Robust.Shared.Utility;

namespace Content.Shared.MedicalScanner;

[RegisterComponent]
public sealed partial class HealthAnalyzerBodyItemComponent : Component
{
    [DataField(required: true)]
    public SpriteSpecifier HoverSprite = default!;

    [DataField(required: true)]
    public SpriteSpecifier SelectedSprite = default!;

    [DataField(required: true)]
    public HealthAnalyzerBodyItemBarPosition ProgressBarLocation = default!;
}
