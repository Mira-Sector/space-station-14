using Content.Client.PolygonRenderer;
using Content.Shared.Arcade.Racer;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Arcade.Racer;

public sealed partial class RacerGameViewportControl : PolygonRendererControl
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    private readonly SpriteSystem _sprite;

    private Entity<RacerArcadeComponent>? _cabinet;

    public RacerGameViewportControl() : base()
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entity.System<SpriteSystem>();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_cabinet is not { } cabinet)
            return;

        var state = cabinet.Comp.State;

        var currentStage = _prototype.Index(state.CurrentStage);
        DrawSky(handle, currentStage.Sky);

        base.Draw(handle);
    }

    private void DrawSky(DrawingHandleScreen handle, RacerGameStageSkyData data)
    {
        // TODO: have this scroll and tile rather than stretch
        var texture = _sprite.Frame0(data.Sprite);
        handle.DrawTextureRect(texture, PixelSizeBox);
    }

    public void SetCabinet(Entity<RacerArcadeComponent> cabinet)
    {
        _cabinet = cabinet;
    }
}
