using Content.Shared.Shadows;
using Content.Shared.Shadows.Components;
using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client.Shadows;

public sealed partial class ShadowSystem : SharedShadowSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private ShadowOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetach);

        SubscribeLocalEvent<HasShadowComponent, ComponentInit>(OnShadowInit);
        SubscribeLocalEvent<HasShadowComponent, ComponentRemove>(OnShadowRemove);

        _overlay = new(EntityManager);
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
}
