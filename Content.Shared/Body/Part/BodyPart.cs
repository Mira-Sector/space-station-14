using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BodyPart
{
    [DataField]
    public BodyPartType Type;

    [DataField]
    public BodyPartSymmetry Side;

    public BodyPart(BodyPartType type, BodyPartSymmetry side)
    {
        Type = type;
        Side = side;
    }
}

[Serializable, NetSerializable]
public enum BodyPartLayer : byte
{
    None,
    Head,
    Torso,
    LArm,
    RArm,
    LLeg,
    RLeg
}
