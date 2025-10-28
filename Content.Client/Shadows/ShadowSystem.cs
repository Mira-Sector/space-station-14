using Content.Shared.Shadows;
using Content.Shared.Shadows.Components;
using Content.Shared.Shadows.Events;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Client.Shadows;

public sealed partial class ShadowSystem : SharedShadowSystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private ShadowOverlay _overlay = default!;
#if DEBUG
    private ShadowDebugOverlay _debugOverlay = default!;
#endif

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetach);

        SubscribeLocalEvent<HasShadowComponent, ComponentInit>(OnShadowInit);
        SubscribeLocalEvent<HasShadowComponent, ComponentRemove>(OnShadowRemove);

        SubscribeLocalEvent<ShadowTreeComponent, ComponentHandleState>(OnGridHandleState);

        SubscribeNetworkEvent<ToggleShadowDebugOverlayEvent>(OnToggleDebug);

        _overlay = new(this, EntityManager, _clyde);
#if DEBUG
        _debugOverlay = new(EntityManager, _random);
#endif
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayManager.RemoveOverlay(_overlay);
        _overlay.Dispose();

#if DEBUG
        _overlayManager.RemoveOverlay(_debugOverlay);
        _debugOverlay.Dispose();
#endif
    }

    private void OnPlayerAttach(LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnPlayerDetach(LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnShadowInit(Entity<HasShadowComponent> ent, ref ComponentInit args)
    {
        _overlay.AddEntity(ent.Owner);
    }

    private void OnShadowRemove(Entity<HasShadowComponent> ent, ref ComponentRemove args)
    {
        _overlay.RemoveEntity(ent.Owner);
    }

    private void OnGridHandleState(Entity<ShadowTreeComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not ShadowTreeState state)
            return;

        ent.Comp.Chunks = state.Chunks;
    }

#if DEBUG
    private void OnToggleDebug(ToggleShadowDebugOverlayEvent args)
    {
        _debugOverlay.ShowCasters = args.ShowCasters;

        if (_overlayManager.HasOverlay<ShadowDebugOverlay>())
            _overlayManager.RemoveOverlay(_debugOverlay);
        else
            _overlayManager.AddOverlay(_debugOverlay);
    }
#endif
}
