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

    public bool Equals(BodyPart other)
    {
        return Type == other.Type && Side == other.Side;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Side);
    }

    public override bool Equals(object? obj)
    {
        return obj is BodyPart other && Equals(other);
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
