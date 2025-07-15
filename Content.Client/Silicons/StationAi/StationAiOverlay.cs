using System.Numerics;
using Content.Shared.Maps;
using Content.Shared.Silicons.StationAi;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly Dictionary<Vector2i, TileRef> _visibleTiles = [];
    private readonly Dictionary<Vector2i, (Vector2, TileRef, IStationAiVisionVisuals)> _tileVisuals = [];
    private readonly Dictionary<Vector2i, List<(Entity<StationAiVisionVisualsComponent, TransformComponent>, IStationAiVisionVisuals)>> _entityVisuals = [];

    private IRenderTexture? _staticTexture;
    private IRenderTexture? _stencilTexture;

    private readonly EntityQuery<StationAiVisionVisualsComponent> _visionVisualsQuery;
    private readonly EntityQuery<AppearanceComponent> _appearanceQuery;
    private readonly EntityQuery<TransformComponent> _xformQuery;

    private static readonly float UpdateRate = 1f / 30f;
    private float _accumulator;

    private const LookupFlags Flags = LookupFlags.Approximate | LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries;

    public StationAiOverlay()
    {
        IoCManager.InjectDependencies(this);

        _visionVisualsQuery = _entManager.GetEntityQuery<StationAiVisionVisualsComponent>();
        _appearanceQuery = _entManager.GetEntityQuery<AppearanceComponent>();
        _xformQuery = _entManager.GetEntityQuery<TransformComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_stencilTexture?.Texture.Size != args.Viewport.Size)
        {
            _staticTexture?.Dispose();
            _stencilTexture?.Dispose();
            _stencilTexture = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "station-ai-stencil");
            _staticTexture = _clyde.CreateRenderTarget(args.Viewport.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: "station-ai-static");
        }

        var worldHandle = args.WorldHandle;

        var worldBounds = args.WorldBounds;

        var playerEnt = _player.LocalEntity;
        _xformQuery.TryGetComponent(playerEnt, out var playerXform);
        var gridUid = playerXform?.GridUid ?? EntityUid.Invalid;
        _entManager.TryGetComponent(gridUid, out MapGridComponent? grid);
        _entManager.TryGetComponent(gridUid, out BroadphaseComponent? broadphase);
        _xformQuery.TryGetComponent(gridUid, out var gridXform);

        var invMatrix = args.Viewport.GetWorldToLocalMatrix();
        _accumulator -= (float)_timing.FrameTime.TotalSeconds;

        var lookups = _entManager.System<EntityLookupSystem>();

        if (grid != null && broadphase != null)
        {
            var appearance = _entManager.System<AppearanceSystem>();
            var xforms = _entManager.System<SharedTransformSystem>();
            var maps = _entManager.System<SharedMapSystem>();

            var gridMatrix = xforms.GetWorldMatrix(gridUid);
            var matty = Matrix3x2.Multiply(gridMatrix, invMatrix);

            if (_accumulator <= 0f)
            {
                _accumulator = MathF.Max(0f, _accumulator + UpdateRate);
                _visibleTiles.Clear();
                _tileVisuals.Clear();
                _entityVisuals.Clear();
                _entManager.System<StationAiVisionSystem>().GetView((gridUid, broadphase, grid), worldBounds, _visibleTiles);

                // get the bb to overcompensate rather than fetch too little incase the camera is rotated
                var worldBoundsMax = worldBounds.CalcBoundingBox();

                var gridInvMatrix = xforms.GetInvWorldMatrix(gridUid);
                var gridAabb = gridInvMatrix.TransformBox(worldBoundsMax);

                HashSet<Vector2i> blockedTiles = [];
                HashSet<Entity<StationAiVisionVisualsComponent>> entities = [];
                lookups.GetLocalEntitiesIntersecting((gridUid, broadphase), gridAabb, entities, _visionVisualsQuery, Flags);
                foreach (var ent in entities)
                {
                    var xform = _xformQuery.GetComponent(ent.Owner);
                    var uidPos = xforms.GetGridTilePositionOrDefault((ent.Owner, xform), grid);

                    if (ent.Comp.BlockTiles)
                        blockedTiles.Add(uidPos);

                    List<(Entity<StationAiVisionVisualsComponent, TransformComponent>, IStationAiVisionVisuals)>? posEnts;
                    if (_appearanceQuery.TryGetComponent(ent.Owner, out var appearanceComp))
                    {
                        var foundAppearanceData = false;

                        foreach (var (@enum, values) in ent.Comp.AppearanceData)
                        {
                            if (!appearance.TryGetData<object>(ent.Owner, @enum, out var data, appearanceComp))
                                continue;

                            var key = data.ToString();
                            if (string.IsNullOrEmpty(key))
                                continue;

                            if (!values.TryGetValue(key, out var visuals))
                                continue;

                            if (!_entityVisuals.TryGetValue(uidPos, out posEnts))
                            {
                                posEnts = [];
                                _entityVisuals[uidPos] = posEnts;
                            }
                            posEnts.Add(((ent.Owner, ent.Comp, xform), visuals));
                            foundAppearanceData = true;
                            break;
                        }

                        if (foundAppearanceData)
                            continue;
                    }


                    if (!_entityVisuals.TryGetValue(uidPos, out posEnts))
                    {
                        posEnts = [];
                        _entityVisuals[uidPos] = posEnts;
                    }
                    posEnts.Add(((ent.Owner, ent.Comp, xform), ent.Comp));
                }

                var chunkEnumerator = new ChunkIndicesEnumerator(gridAabb, grid.TileSize);
                while (chunkEnumerator.MoveNext(out var tileIndices))
                {
                    if (blockedTiles.Contains(tileIndices.Value))
                        continue;

                    if (!maps.TryGetTileRef(gridUid, grid, tileIndices.Value, out var tile))
                        continue;

                    var tileDef = tile.GetContentTileDefinition(_tileDefinitions);

                    if (tileDef.StationAiVisuals is not { } aiVisuals)
                        continue;

                    var tilePos = maps.GridTileToWorld(gridUid, grid, tileIndices.Value).Position;
                    _tileVisuals[tileIndices.Value] = (tilePos, tile, aiVisuals);
                }
            }

            // Draw visible tiles to stencil
            worldHandle.RenderInRenderTarget(_stencilTexture!, () =>
            {
                worldHandle.SetTransform(matty);

                foreach (var tile in _visibleTiles.Keys)
                {
                    var aabb = lookups.GetLocalBounds(tile, grid.TileSize);
                    worldHandle.DrawRect(aabb, Color.White);
                }
            },
            Color.Transparent);

            // Once this is gucci optimise rendering.
            worldHandle.RenderInRenderTarget(_staticTexture!,
            () =>
            {
                worldHandle.SetTransform(invMatrix);
                var shader = _proto.Index<ShaderPrototype>("CameraStatic").Instance();
                worldHandle.UseShader(shader);
                worldHandle.DrawRect(worldBounds, Color.White);
            },
            Color.Black);

            DrawStencils(worldHandle, worldBounds);

            foreach (var (tileIndices, (pos, tileRef, aiVisuals)) in _tileVisuals)
            {
                if (_visibleTiles.ContainsKey(tileIndices))
                    continue;

                var tileOffset = Matrix3x2.CreateTranslation(tileIndices * grid.TileSizeVector);
                var transform = Matrix3x2.Multiply(tileOffset, gridMatrix);
                worldHandle.SetTransform(transform);
                DrawVisuals(aiVisuals, worldHandle);
            }

            var eyeRot = args.Viewport.Eye?.Rotation.Reduced() ?? Angle.Zero;
            var gridRot = gridXform?.LocalRotation ?? Angle.Zero;
            foreach (var (tileIndices, ents) in _entityVisuals)
            {
                foreach (var (ent, aiVisuals) in ents)
                {
                    // no need to check if the tile is visible as the entity lookup only checks blocked tiles
                    var (worldPos, worldRot) = xforms.GetWorldPositionRotation(ent.Comp2, _xformQuery);
                    var rot = ent.Comp1.NoRotation ? -eyeRot : worldRot;
                    if (ent.Comp1.SnapCardinals)
                        rot = gridRot + rot.RoundToCardinalAngle();
                    var pos = worldPos - rot.RotateVec(grid.TileSizeHalfVector);
                    var transform = Matrix3Helpers.CreateTransform(pos, rot);
                    worldHandle.SetTransform(transform);
                    DrawVisuals(aiVisuals, worldHandle);
                }
            }
        }
        // Not on a grid
        else
        {
            worldHandle.RenderInRenderTarget(_stencilTexture!, () =>
            {
            },
            Color.Transparent);

            worldHandle.RenderInRenderTarget(_staticTexture!,
            () =>
            {
                worldHandle.SetTransform(Matrix3x2.Identity);
                worldHandle.DrawRect(worldBounds, Color.Black);
            }, Color.Black);

            DrawStencils(worldHandle, worldBounds);
        }

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(null);
    }

    private void DrawStencils(DrawingHandleWorld worldHandle, Box2Rotated worldBounds)
    {
        // Use the lighting as a mask
        worldHandle.UseShader(_proto.Index<ShaderPrototype>("StencilMask").Instance());
        worldHandle.DrawTextureRect(_stencilTexture!.Texture, worldBounds);

        // Draw the static
        worldHandle.UseShader(_proto.Index<ShaderPrototype>("StencilDraw").Instance());
        worldHandle.DrawTextureRect(_staticTexture!.Texture, worldBounds);
    }

    private static void DrawVisuals(IStationAiVisionVisuals visuals, DrawingHandleWorld worldHandle)
    {
        foreach (var shape in visuals.Shapes)
            ((IClientStationAiVisionVisualsShape)shape).Draw(worldHandle);
    }
}
