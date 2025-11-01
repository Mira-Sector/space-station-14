#if DEBUG
using Robust.Shared.Serialization;

namespace Content.Shared.Shadows.Events;

[Serializable, NetSerializable]
public sealed partial class ToggleShadowDebugOverlayEvent(bool showCasters) : EntityEventArgs
{
    public readonly bool ShowCasters = showCasters;
}
#endif
