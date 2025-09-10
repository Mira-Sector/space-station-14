using Content.Shared.Maps;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Numerics;

namespace Content.Client.UserInterface.Controls;

public sealed partial class GridView : Control
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitions = default!;
    [Dependency] private readonly IResourceCache _resource = default!;
    private readonly SharedContainerSystem _container;
    private readonly EntityLookupSystem _lookup;
    private readonly MapSystem _map;
    private readonly SharedTransformSystem _transform;

    [ViewVariables]
    public Entity<MapGridComponent, TransformComponent>? Grid { get; private set; }

    private readonly Dictionary<Tile, Dictionary<byte, Texture>> _tileVariations = [];

    public GridView()
    {
        IoCManager.InjectDependencies(this);

        _container = _entity.System<SharedContainerSystem>();
        _lookup = _entity.System<EntityLookupSystem>();
        _map = _entity.System<MapSystem>();
        _transform = _entity.System<SharedTransformSystem>();
    }

    public void SetGrid(EntityUid? grid)
    {
        if (grid == null)
            Grid = null;
        else if (_entity.TryGetComponent<MapGridComponent>(grid, out var gridComp))
            SetGrid((grid.Value, gridComp));
    }

    public void SetGrid(Entity<MapGridComponent> grid)
    {
        var xform = _entity.GetComponent<TransformComponent>(grid.Owner);
        Grid = (grid.Owner, grid.Comp, xform);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (Grid is not { } grid)
            return;

        var aabb = grid.Comp1.LocalAABB;

        var transform = Matrix3x2.Identity;

        // not centered
        if (aabb.TopLeft.LengthSquared() > 0f)
        {
            var offset = -aabb.TopLeft;
            transform = Matrix3x2.CreateTranslation(offset);
        }

        float scale;
        if (aabb.Width > aabb.Height)
            scale = aabb.Height / aabb.Width;
        else
            scale = aabb.Width / aabb.Height;

        transform *= Matrix3x2.CreateScale(scale);
        handle.SetTransform(transform);

        DrawTiles(handle);
        DrawEntities(handle);

        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawTiles(DrawingHandleScreen handle)
    {
        var grid = Grid!.Value;

        var tiles = _map.GetAllTilesEnumerator(grid.Owner, grid.Comp1);
        while (tiles.MoveNext(out var nextTile))
        {
            var tile = nextTile.Value;
            var tileDef = tile.Tile.GetContentTileDefinition(_tileDefinitions);

            if (tileDef.Sprite is not { } tileSpritePath)
                continue;

            var localBounds = _lookup.GetLocalBounds(tile, grid.Comp1.TileSize);
            var bounds = UIBox2.FromDimensions(localBounds.TopLeft, localBounds.Size);

            if (!_tileVariations.TryGetValue(tile.Tile, out var variants) || !variants.TryGetValue(tile.Tile.Variant, out var texture))
            {
                var atlasTexture = _resource.GetResource<TextureResource>(tileSpritePath);

                if (tileDef.Variants == 1)
                {
                    texture = atlasTexture;
                }
                else
                {
                    var variant = tile.Tile.Variant + 1;
                    var size = atlasTexture.Texture.Size.X / tileDef.Variants;

                    var variantBounds = UIBox2.FromDimensions(variant * size - size, 0, size, atlasTexture.Texture.Size.Y);
                    texture = new AtlasTexture(atlasTexture, variantBounds);
                }

                if (!_tileVariations.ContainsKey(tile.Tile))
                    _tileVariations.Add(tile.Tile, []);

                _tileVariations[tile.Tile].Add(tile.Tile.Variant, texture);
            }

            // TODO: decals???
            handle.DrawTextureRect(texture, bounds);
        }
    }

    private void DrawEntities(DrawingHandleScreen handle)
    {
        var grid = Grid!.Value;

        var entities = grid.Comp2.ChildEnumerator;
        while (entities.MoveNext(out var entity))
        {
            var xform = _entity.GetComponent<TransformComponent>(entity);
            if (_container.IsEntityOrParentInContainer(entity, xform: xform))
                continue;

            var pos = xform.LocalPosition;
            var rot = xform.LocalRotation;
            handle.DrawEntity(entity, pos, Vector2.One, rot, Angle.Zero, xform: xform, xformSystem: _transform);
        }
    }
}
