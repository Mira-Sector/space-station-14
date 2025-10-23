#if DEBUG
using Content.Shared.Shadows;
using Content.Shared.Shadows.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;

namespace Content.Client.Shadows;

public sealed partial class ShadowDebugOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly IEntityManager _entity;
    private readonly IRobustRandom _random;
    private readonly EntityLookupSystem _lookup;
    private readonly TransformSystem _transform;

    public ShadowDebugOverlay(IEntityManager entity, IRobustRandom random) : base()
    {
        _entity = entity;
        _random = random;
        _lookup = _entity.System<EntityLookupSystem>();
        _transform = _entity.System<TransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var gridQuery = _entity.EntityQueryEnumerator<ShadowGridComponent, MapGridComponent>();
        while (gridQuery.MoveNext(out var uid, out var shadow, out var grid))
        {
            var matty = _transform.GetWorldMatrix(uid);
            args.WorldHandle.SetTransform(matty);

            foreach (var chunk in shadow.Chunks.Values)
            {
                foreach (var (indices, data) in chunk.ShadowMap)
                {
                    var bounds = _lookup.GetLocalBounds(indices, grid.TileSize);
                    var alpha = 1f - data.Strength;
                    var color = ShadowData.Color.WithAlpha(alpha);
                    args.WorldHandle.DrawRect(bounds, color);

                    var start = bounds.Center;
                    var end = start + data.Direction / 2f;
                    args.WorldHandle.DrawLine(start, end, Color.Blue);
                }

                var chunkBounds = new Box2(
                    chunk.ChunkPos.X * ShadowGridComponent.ChunkSize,
                    chunk.ChunkPos.Y * ShadowGridComponent.ChunkSize,
                    (chunk.ChunkPos.X + 1) * ShadowGridComponent.ChunkSize,
                    (chunk.ChunkPos.Y + 1) * ShadowGridComponent.ChunkSize
                );
                args.WorldHandle.DrawRect(chunkBounds, Color.Red, false);
            }
        }

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
            args.WorldHandle.SetTransform(matty);

            var posIndices = new Vector2i((int)MathF.Round(xform.LocalPosition.X), (int)MathF.Round(xform.LocalPosition.Y));

            foreach (var (indices, data) in caster.ShadowMap)
            {
                var actualIndices = posIndices + indices;
                var bounds = _lookup.GetLocalBounds(actualIndices, gridComp.TileSize);
                var start = bounds.Center;
                var end = start + data.Direction / 2f;
                args.WorldHandle.DrawLine(start, end, color);
            }
        }

        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }
}
#endif
