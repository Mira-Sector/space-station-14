using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Atmos.Piping.Crawling.Systems;
using Robust.Client.Graphics;

namespace Content.Client.Atmos.Piping.Crawling;

public sealed partial class PipeCrawlingSystem : SharedPipeCrawlingSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    private PipeCrawlingOverlay _crawlingOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _crawlingOverlay = new();
    }

    protected override void UpdateOverlay(Entity<PipeCrawlingComponent> ent)
    {
        if (!_overlay.HasOverlay<PipeCrawlingOverlay>())
            _overlay.AddOverlay(_crawlingOverlay);

        _crawlingOverlay.Crawler = ent;
    }

    protected override void RemoveOverlay(Entity<PipeCrawlingComponent> ent)
    {
        if (_crawlingOverlay.Crawler != ent)
            return;

        _overlay.RemoveOverlay(_crawlingOverlay);
    }
}
