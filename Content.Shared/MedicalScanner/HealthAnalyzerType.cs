using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner;

[Serializable, NetSerializable]
[Flags]
public enum HealthAnalyzerType : byte
{
    Body = 1 << 0,
    Organs = 1 << 1,

    BodyAndOrgans = Body | Organs
}
