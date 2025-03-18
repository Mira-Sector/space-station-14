using Content.Shared.Body.Part;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Numerics;

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
    [DataField]
    public SpriteSpecifier? Sprite;

    [DataField]
    public Vector2 Offset;
}
