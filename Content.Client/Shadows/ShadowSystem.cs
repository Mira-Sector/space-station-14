using Content.Shared.Shadows;
using Content.Shared.Shadows.Components;
using Content.Shared.Shadows.Events;
using Robust.Client.Graphics;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Client.Shadows;

public sealed partial class ShadowSystem : SharedShadowSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private ShadowOverlay _overlay = default!;
    private ShadowDebugOverlay _debugOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetach);

        SubscribeLocalEvent<HasShadowComponent, ComponentInit>(OnShadowInit);
        SubscribeLocalEvent<HasShadowComponent, ComponentRemove>(OnShadowRemove);

        SubscribeNetworkEvent<ToggleShadowDebugOverlayEvent>(OnToggleDebug);

        _overlay = new(EntityManager);
        _debugOverlay = new(EntityManager, _random);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayManager.RemoveOverlay(_overlay);
        _overlay.Dispose();

        _overlayManager.RemoveOverlay(_debugOverlay);
        _debugOverlay.Dispose();
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

    private void OnToggleDebug(ToggleShadowDebugOverlayEvent args)
    {
        if (_overlayManager.HasOverlay<ShadowDebugOverlay>())
            _overlayManager.RemoveOverlay(_debugOverlay);
        else
            _overlayManager.AddOverlay(_debugOverlay);
    }
}
