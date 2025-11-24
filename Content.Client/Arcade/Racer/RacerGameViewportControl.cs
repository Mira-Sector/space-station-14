using Content.Client.PolygonRenderer;
using Content.Shared.Arcade.Racer;
using Content.Shared.Arcade.Racer.Stage;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Arcade.Racer;

public sealed partial class RacerGameViewportControl : PolygonRendererControl
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    private readonly SpriteSystem _sprite;

    private Entity<RacerArcadeComponent>? _cabinet;

    private const int RenderableEdgeBezierSamples = 8;
    private const float RenderageEdgeDrawDistance = 2048f;

    public RacerGameViewportControl() : base()
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entity.System<SpriteSystem>();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_cabinet is not { } cabinet)
            return;

        Models = [];

        var state = cabinet.Comp.State;

        var currentStage = _prototype.Index(state.CurrentStage);
        DrawSky(handle, currentStage.Sky);
        DrawGraph(currentStage.Graph, state.CurrentNode);

        SetCameraMatrix(currentStage.Graph, state.CurrentNode);

        base.Draw(handle);
    }

    private void DrawSky(DrawingHandleScreen handle, RacerGameStageSkyData data)
    {
        // TODO: have this scroll and tile rather than stretch
        var texture = _sprite.Frame0(data.Sprite);
        handle.DrawTextureRect(texture, PixelSizeBox);
    }

    private void DrawGraph(RacerArcadeStageGraph graph, RacerArcadeStageNode currentNode)
    {
        var trackModel = GraphToPolygonModel(graph, RenderableEdgeBezierSamples, RenderageEdgeDrawDistance, currentNode.Position);
        Models.Add(trackModel);
    }

    private void SetCameraMatrix(RacerArcadeStageGraph graph, RacerArcadeStageNode currentNode)
    {
        Camera = Matrix4.LookAt(currentNode.Position, Vector3.Zero, Vector3.UnitZ);
    }

    public void SetCabinet(Entity<RacerArcadeComponent> cabinet)
    {
        _cabinet = cabinet;
    }
}
