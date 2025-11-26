using Content.Client.PolygonRenderer;
using Content.Client.Arcade.Racer.Systems;
using Content.Shared.Arcade.Racer;
using Content.Shared.Arcade.Racer.Components;
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
    private readonly RacerArcadeSystem _racer;

    private Entity<RacerArcadeComponent>? _cabinet;
    private EntityUid? _viewer;

    private const int RenderableEdgeBezierSamples = 8;
    private const float RenderageEdgeDrawDistance = 2048f;

    public RacerGameViewportControl() : base()
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entity.System<SpriteSystem>();
        _racer = _entity.System<RacerArcadeSystem>();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_cabinet is not { } cabinet || _viewer is not { } viewer)
            return;

        Models = [];

        var state = cabinet.Comp.State;

        var currentStage = _prototype.Index(state.CurrentStage);
        DrawSky(handle, currentStage.Sky);
        DrawGraph(currentStage.Graph, state.CurrentNode);
        DrawObjects(state.Objects);

        SetCameraMatrix(currentStage.Graph, state.CurrentNode, cabinet, viewer);

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

    private void DrawObjects(List<NetEntity> objects)
    {
        Models.EnsureCapacity(Models.Count + objects.Count);
        foreach (var netObj in objects)
        {
            if (!_entity.TryGetEntity(netObj, out var obj))
                continue;

            if (!_entity.TryGetComponent<RacerArcadeObjectModelComponent>(obj, out var objModel))
                continue;

            var objData = _entity.GetComponent<RacerArcadeObjectComponent>(obj.Value);

            var model = _prototype.Index(objModel.Model);
            model.ModelMatrix = Matrix4.Rotate(objData.Rotation) * Matrix4.CreateTranslation(objData.Position);
            Models.Add(model);
        }
    }

    private void SetCameraMatrix(RacerArcadeStageGraph graph, RacerArcadeStageNode currentNode, Entity<RacerArcadeComponent> cabinet, EntityUid viewer)
    {
        if (!_racer.TryGetControlledObject(cabinet!, viewer, out var controlled))
        {
            Camera = Matrix4.LookAt(currentNode.Position, Vector3.Zero, Vector3.UnitZ);
            return;
        }

        /*
         * wipeout style, duhh
         *
         * camera is positioned between the track center and the player
        */
        var data = _entity.GetComponent<RacerArcadeObjectComponent>(controlled.Value.Owner);

        var trackCenter = GetClosestPointOnTrack(graph, data.Position, RenderableEdgeBezierSamples);

        var rotMatrix = Matrix4.Rotate(data.Rotation);
        var forward = Vector3.TransformNormal(Vector3.UnitX, rotMatrix);
        var right = Vector3.Cross(forward, Vector3.UnitZ);

        var eye = data.Position
            + forward * controlled.Value.Comp.CameraOffset.X
            + right * controlled.Value.Comp.CameraOffset.Y
            + Vector3.UnitZ * controlled.Value.Comp.CameraOffset.Z;

        var lookTarget = data.Position + forward;
        lookTarget = Vector3.Lerp(lookTarget, trackCenter, 0.5f);
        Camera = Matrix4.LookAt(eye, lookTarget, Vector3.UnitZ);
    }

    public void SetCabinet(Entity<RacerArcadeComponent> cabinet, EntityUid viewer)
    {
        _cabinet = cabinet;
        _viewer = viewer;
    }
}
