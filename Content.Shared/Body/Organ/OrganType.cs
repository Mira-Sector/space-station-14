using Robust.Shared.Serialization;

namespace Content.Shared.Body.Organ;

[Serializable, NetSerializable]
public enum OrganType
{
    Other = 0,
    Brain,
    Tongue,
    Ears,
    Heart,
    Lungs,
    Eyes,
    Liver,
    Stomach,
    Kidneys,
    Appendix
}
