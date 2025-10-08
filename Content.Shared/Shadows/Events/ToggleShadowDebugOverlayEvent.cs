using Robust.Shared.Serialization;

namespace Content.Shared.Shadows.Events;

[Serializable, NetSerializable]
public sealed partial class ToggleShadowDebugOverlayEvent : EntityEventArgs;
