using Content.Shared.Atmos.Piping.Crawling.Components;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public sealed class PipeCrawlingPipeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeCrawlingPipeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PipeCrawlingPipeComponent, AnchorStateChangedEvent>(OnAnchored);
    }

    private void OnInit(EntityUid uid, PipeCrawlingPipeComponent component, ref ComponentInit args)
    {
        UpdateState(uid, component);
    }

    private void OnAnchored(EntityUid uid, PipeCrawlingPipeComponent component, ref AnchorStateChangedEvent args)
    {
        UpdateState(uid, component);
    }

    private void UpdateState(EntityUid uid, PipeCrawlingPipeComponent component)
    {
        component.Enabled = Comp<TransformComponent>(uid).Anchored;
    }
}
