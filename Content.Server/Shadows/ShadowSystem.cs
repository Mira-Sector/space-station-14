using Content.Shared.Shadows;
using Content.Shared.Shadows.Events;
using Robust.Shared.Player;

namespace Content.Server.Shadows;

public sealed partial class ShadowSystem : SharedShadowSystem
{
#if DEBUG
    public void ToggleDebugOverlay(ICommonSession session)
    {
        var ev = new ToggleShadowDebugOverlayEvent();
        RaiseNetworkEvent(ev, session);
    }
#endif
}
