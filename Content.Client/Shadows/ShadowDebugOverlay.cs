#if DEBUG
using Content.Client.Resources;
using Content.Shared.Shadows;
using Content.Shared.Shadows.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;

namespace Content.Client.Shadows;

public sealed partial class ShadowDebugOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;

    public bool ShowCasters = false;

    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private readonly IEntityManager _entity;
    private readonly EntityLookupSystem _lookup;
    private readonly SharedMapSystem _map;
    private readonly TransformSystem _transform;

    private readonly Font _font;
    private List<Entity<ShadowTreeComponent, MapGridComponent>> _grids = [];

    public ShadowDebugOverlay(IEntityManager entity) : base()
    {
        IoCManager.InjectDependencies(this);

        _entity = entity;
        _lookup = _entity.System<EntityLookupSystem>();
        _map = _entity.System<MapSystem>();
        _transform = _entity.System<TransformSystem>();

        _font = _cache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 12);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Space == OverlaySpace.WorldSpace)
        {
            DrawGrids(args.WorldHandle, args.MapId, args.WorldBounds);

            if (ShowCasters)
                DrawCasters(args.WorldHandle);

            args.WorldHandle.SetTransform(Matrix3x2.Identity);
        }
        else if (args.Space == OverlaySpace.ScreenSpace)
        {
            args.ScreenHandle.SetTransform(Matrix3x2.Identity);
            DrawTooltips(args.ScreenHandle);
        }
    }

    private void DrawGrids(DrawingHandleWorld worldHandle, MapId mapId, Box2Rotated worldBounds)
    {
        GetGrids(mapId, worldBounds);
        foreach (var grid in _grids)
        {
            var matty = _transform.GetWorldMatrix(grid.Owner);
            worldHandle.SetTransform(matty);

            foreach (var chunk in grid.Comp1.Chunks.Values)
            {
                foreach (var (indices, data) in chunk.ShadowMap)
                {
                    var bounds = _lookup.GetLocalBounds(indices, grid.Comp2.TileSize);
                    var alpha = 1f - data.Strength;
                    var color = ShadowData.Color.WithAlpha(alpha);
                    worldHandle.DrawRect(bounds, color);

                    var start = bounds.Center;
                    var end = start + data.Direction / 2f;
                    worldHandle.DrawLine(start, end, Color.Blue);
                }

                var chunkBounds = new Box2(
                    chunk.ChunkPos.X * ShadowTreeComponent.ChunkSize,
                    chunk.ChunkPos.Y * ShadowTreeComponent.ChunkSize,
                    (chunk.ChunkPos.X + 1) * ShadowTreeComponent.ChunkSize,
                    (chunk.ChunkPos.Y + 1) * ShadowTreeComponent.ChunkSize
                );
                worldHandle.DrawRect(chunkBounds, Color.Red, false);
            }
        }
    }

    private void DrawCasters(DrawingHandleWorld worldHandle)
    {
        var colors = Color.GetAllDefaultColors().ToList();

        var casterQuery = _entity.EntityQueryEnumerator<ShadowCasterComponent, TransformComponent>();
        while (casterQuery.MoveNext(out var uid, out var caster, out var xform))
        {
            if (xform.GridUid is not { } grid)
                continue;

            _random.SetSeed(uid.Id); // stop flickering per frame, uid never changes
            var color = _random.PickAndTake(colors).Value;
            if (!colors.Any())
                colors = Color.GetAllDefaultColors().ToList();

            var gridComp = _entity.GetComponent<MapGridComponent>(grid);

            var matty = _transform.GetWorldMatrix(grid);
            worldHandle.SetTransform(matty);

            var posIndices = new Vector2i((int)MathF.Round(xform.LocalPosition.X), (int)MathF.Round(xform.LocalPosition.Y));

            foreach (var (indices, data) in caster.ShadowMap)
            {
                var actualIndices = posIndices + indices;
                var bounds = _lookup.GetLocalBounds(actualIndices, gridComp.TileSize);
                var start = bounds.Center;
                var end = start + data.Direction / 2f;
                worldHandle.DrawLine(start, end, color);
            }
        }
    }

    // shamelessly ripped straight from atmos debug overlay
    private void DrawTooltips(DrawingHandleScreen screenHandle)
    {
        var mousePos = _input.MouseScreenPosition;
        if (!mousePos.IsValid)
            return;

        if (_ui.MouseGetControl(mousePos) is not IViewportControl viewport)
            return;

        var coords = viewport.PixelToMap(mousePos.Position);
        var box = new Box2Rotated(Box2.CenteredAround(coords.Position, 3 * Vector2.One));

        GetGrids(coords.MapId, box);
        foreach (var grid in _grids)
        {
            var tilePos = _map.WorldToTile(grid, grid, coords.Position);
            var chunkPos = new Vector2i(tilePos.X / ShadowTreeComponent.ChunkSize, tilePos.Y / ShadowTreeComponent.ChunkSize);

            if (!grid.Comp1.Chunks.TryGetValue(chunkPos, out var chunk))
                continue;

            var data = chunk.ShadowMap[tilePos];
            DrawTooltip(screenHandle, mousePos.Position, data, tilePos, chunkPos);
        }
    }

    private void DrawTooltip(DrawingHandleScreen screenHandle, Vector2 pos, ShadowData data, Vector2i tilePos, Vector2i chunkPos)
    {
        var lineHeight = _font.GetLineHeight(1f);
        var offset = new Vector2(0, lineHeight);

        screenHandle.DrawString(_font, pos, $"Direction: {data.Direction}");
        pos += offset;
        screenHandle.DrawString(_font, pos, $"Strength: {data.Strength}");
        pos += offset;
        screenHandle.DrawString(_font, pos, $"Tile Position: {tilePos}");
        pos += offset;
        screenHandle.DrawString(_font, pos, $"Chunk: {chunkPos}");
    }

    private void GetGrids(MapId mapId, Box2Rotated box)
    {
        _grids.Clear();

        _mapManager.FindGridsIntersecting(
            mapId,
            box,
            ref _grids,
            (EntityUid uid,
                MapGridComponent grid,
            ref List<Entity<ShadowTreeComponent, MapGridComponent>> state) =>
        {
            if (!_entity.TryGetComponent<ShadowTreeComponent>(uid, out var tree))
                return false;

            state.Add((uid, tree, grid));
            return true;

        });
    }
}
#endif
