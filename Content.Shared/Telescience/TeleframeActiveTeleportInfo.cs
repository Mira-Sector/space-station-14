using Robust.Shared.Serialization;

namespace Content.Shared.Telescience;

[Serializable, NetSerializable]
public readonly record struct TeleframeActiveTeleportInfo(TeleframeActivationMode Mode, NetEntity To, NetEntity From);
