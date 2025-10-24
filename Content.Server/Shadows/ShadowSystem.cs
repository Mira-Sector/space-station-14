using Content.Shared.Shadows;
using Content.Shared.Shadows.Components;
using Content.Shared.Shadows.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Server.Shadows;

public sealed partial class ShadowSystem : SharedShadowSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowGridComponent, ComponentGetState>(OnGridGetState);
    }

    private void OnGridGetState(Entity<ShadowGridComponent> ent, ref ComponentGetState args)
    {
        args.State = new ShadowGridState(GetNetEntitySet(ent.Comp.Casters), ent.Comp.Chunks);
    }

#if DEBUG
    public void ToggleDebugOverlay(ICommonSession session)
    {
        var ev = new ToggleShadowDebugOverlayEvent();
        RaiseNetworkEvent(ev, session);
    }
#endif
}
