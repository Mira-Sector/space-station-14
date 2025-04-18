using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Sync;

[Serializable, NetSerializable]
public enum SiliconSyncCommandingPathType
{
    NoPath,
    PathFound,
    Moving
}
