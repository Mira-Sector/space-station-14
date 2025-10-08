using Content.Shared.Shadows;
using Content.Shared.Shadows.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using System.Numerics;

namespace Content.Client.Shadows;

public sealed partial class ShadowDebugOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly IEntityManager _entity;
    private readonly EntityLookupSystem _lookup;
    private readonly TransformSystem _transform;

    public ShadowDebugOverlay(IEntityManager entity) : base()
    {
        _entity = entity;
        _lookup = _entity.System<EntityLookupSystem>();
        _transform = _entity.System<TransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entity.EntityQueryEnumerator<ShadowGridComponent, MapGridComponent>();
        while (query.MoveNext(out var uid, out var shadow, out var grid))
        {
            var matty = _transform.GetWorldMatrix(uid);
            args.WorldHandle.SetTransform(matty);

            foreach (var (indices, data) in shadow.ShadowMap)
            {
                var bounds = _lookup.GetLocalBounds(indices, grid.TileSize);
                var alpha = 1f - data.Strength;
                var color = ShadowData.Color.WithAlpha(alpha);
                args.WorldHandle.DrawRect(bounds, color);

                var center = bounds.Center;
                args.WorldHandle.DrawLine(center, center + data.Direction, Color.Blue);
            }
        }

        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }
}
