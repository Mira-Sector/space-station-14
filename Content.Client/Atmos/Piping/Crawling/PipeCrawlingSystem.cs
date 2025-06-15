using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Atmos.Piping.Crawling.Systems;
using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client.Atmos.Piping.Crawling;

public sealed partial class PipeCrawlingSystem : SharedPipeCrawlingSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    private PipeCrawlingOverlay _crawlingOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnDetached);

        _crawlingOverlay = new();
    }

    private void OnAttached(LocalPlayerAttachedEvent args)
    {
        UpdateOverlay(args.Entity);
    }

    private void OnDetached(LocalPlayerDetachedEvent args)
    {
        RemoveOverlay(args.Entity);
    }

    protected override void UpdateOverlay(Entity<PipeCrawlingComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!CanDrawOverlay(ent!))
            return;

        if (!_overlay.HasOverlay<PipeCrawlingOverlay>())
            _overlay.AddOverlay(_crawlingOverlay);

        _crawlingOverlay.Crawler = ent!;
    }

    protected override void RemoveOverlay(Entity<PipeCrawlingComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!CanDrawOverlay(ent!))
            return;

        if (_crawlingOverlay.Crawler != ent!)
            return;

        _overlay.RemoveOverlay(_crawlingOverlay);
    }

    private bool CanDrawOverlay(Entity<PipeCrawlingComponent> ent)
    {
        if (Player.LocalSession?.AttachedEntity != ent.Owner)
            return false;

        return true;
    }
}
