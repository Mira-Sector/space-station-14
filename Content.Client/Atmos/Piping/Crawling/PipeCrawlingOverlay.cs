using Content.Shared.Atmos.Piping.Crawling.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Atmos.Piping.Crawling;

public sealed class PipeCrawlingOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public Entity<PipeCrawlingComponent>? Crawler;

    private const float Alpha = 0.6f;

    public PipeCrawlingOverlay()
    {
        IoCManager.InjectDependencies(this);
        _sprite = _entityManager.System<SpriteSystem>();
        _transform = _entityManager.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (Crawler == null)
            return;

        var eyeRot = args.Viewport.Eye?.Rotation ?? Angle.Zero;

        if (!_entityManager.TryGetComponent<SpriteComponent>(Crawler.Value.Owner, out var sprite))
            return;

        var oldColor = sprite.Color;
        _sprite.SetColor((Crawler.Value.Owner, sprite), sprite.Color.WithAlpha(sprite.Color.A * Alpha));

        var pipePos = _transform.GetWorldPosition(Crawler.Value.Comp.CurrentPipe);
        var worldRot = _transform.GetWorldRotation(Crawler.Value.Owner);
        _sprite.RenderSprite((Crawler.Value.Owner, sprite), args.WorldHandle, eyeRot, worldRot, pipePos);

        _sprite.SetColor((Crawler.Value.Owner, sprite), oldColor);
    }
}
