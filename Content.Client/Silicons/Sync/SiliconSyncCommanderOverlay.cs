using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;

namespace Content.Client.Silicons.Sync;

public sealed class SiliconSyncCommanderOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly EntityLookupSystem _lookups;
    private readonly SharedMapSystem _map;
    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    public Dictionary<EntityUid, (KeyValuePair<NetCoordinates, Direction>[], SpriteSpecifier)> Paths = new();
    private Dictionary<SpriteSpecifier, (int Frame, TimeSpan NextFrame)> Sprites = new();
    private readonly ShaderInstance _shader;

    public SiliconSyncCommanderOverlay()
    {
        IoCManager.InjectDependencies(this);
        _lookups = _entityManager.System<EntityLookupSystem>();
        _map = _entityManager.System<SharedMapSystem>();
        _sprite = _entityManager.System<SpriteSystem>();
        _transform = _entityManager.System<SharedTransformSystem>();

        _shader = _prototype.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return Paths.Any();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_shader);

        HashSet<TileRef> drawnTiles = new();

        foreach (var (_, (path, sprite)) in Paths)
        {
            var state = _sprite.RsiStateLike(sprite);

            if (!Sprites.TryGetValue(sprite, out var durations))
            {
                durations = (0, _timing.CurTime + TimeSpan.FromSeconds(state.GetDelay(0)));
                Sprites.Add(sprite, durations);
            }

            foreach (var (netPos, direction) in path)
            {
                var pos = _entityManager.GetCoordinates(netPos);
                if (!_entityManager.TryGetComponent<MapGridComponent>(pos.EntityId, out var mapGridComp))
                    continue;

                var gridMatrix = _transform.GetWorldMatrix(pos.EntityId);
                worldHandle.SetTransform(gridMatrix);

                var texture = state.GetFrame(DirExt.Convert(direction, state.RsiDirections), durations.Frame);

                var tile = _map.GetTileRef((pos.EntityId, mapGridComp), pos);

                // dont draw multiple on the same tile
                if (!drawnTiles.Add(tile))
                    continue;

                var bounds = _lookups.GetLocalBounds(tile, mapGridComp.TileSize);
                worldHandle.DrawTextureRect(texture, bounds);
            }
        }

        worldHandle.UseShader(null);
        worldHandle.SetTransform(Matrix3x2.Identity);

        Dictionary<SpriteSpecifier, (int, TimeSpan)> updatedSprites = new();

        foreach (var (sprite, (frame, nextFrame)) in Sprites)
        {
            var state = _sprite.RsiStateLike(sprite);

            if (!state.IsAnimated)
                continue;

            if (nextFrame > _timing.CurTime)
                continue;

            var newFrame = frame + 1;

            if (newFrame >= state.AnimationFrameCount)
                newFrame = 0;

            updatedSprites.Add(sprite, (newFrame, nextFrame + TimeSpan.FromSeconds(state.GetDelay(frame))));
        }

        foreach (var (sprite, data) in updatedSprites)
            Sprites[sprite] = data;
    }
}
