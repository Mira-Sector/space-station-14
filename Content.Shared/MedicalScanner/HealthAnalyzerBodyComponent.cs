using Content.Shared.Body.Part;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.MedicalScanner;

[RegisterComponent]
public sealed partial class HealthAnalyzerBodyComponent : Component
{
    [DataField]
    public Dictionary<BodyPart, HealthAnalyzerLimb> Limbs = new();
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class HealthAnalyzerLimb
{
    [DataField(required: true)]
    public SpriteSpecifier HoverSprite = default!;

    [DataField(required: true)]
    public SpriteSpecifier SelectedSprite = default!;
}
