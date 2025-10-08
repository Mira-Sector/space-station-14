using Content.Shared.Shadows;
using Content.Shared.Shadows.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using System.Linq;
using System.Numerics;

namespace Content.Client.Shadows;

public sealed partial class ShadowOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    private readonly IEntityManager _entity;
    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _xform;

    private readonly EntityQuery<SpriteComponent> _spriteQuery;
    private readonly EntityQuery<ShadowGridComponent> _gridQuery;
    private readonly EntityQuery<TransformComponent> _xformQuery;

    private readonly HashSet<EntityUid> _toRender = [];

    public ShadowOverlay(IEntityManager entity) : base()
    {
        ZIndex = 100;

        _entity = entity;
        _sprite = _entity.System<SpriteSystem>();
        _xform = _entity.System<TransformSystem>();
        _spriteQuery = _entity.GetEntityQuery<SpriteComponent>();
        _gridQuery = _entity.GetEntityQuery<ShadowGridComponent>();
        _xformQuery = _entity.GetEntityQuery<TransformComponent>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _toRender.Any();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach (var target in _toRender)
        {
            if (!_spriteQuery.TryComp(target, out var sprite))
                continue;

            var xform = _xformQuery.GetComponent(target);
            if (!_gridQuery.TryComp(xform.GridUid, out var grid))
                continue;

            var eyeRot = args.Viewport.Eye?.Rotation ?? Angle.Zero;
            var worldPos = _xform.GetWorldPosition(xform);
            var worldRot = _xform.GetWorldRotation(xform);

            if (GetInterpulatedShadow((target, xform), (xform.GridUid.Value, grid)) is not { } data)
                continue;

            var angle = MathF.Atan2(data.Direction.Y, data.Direction.X);

            var prevColor = sprite.Color;
            var prevMatty = sprite.LocalMatrix;

            var bounds = _sprite.GetLocalBounds((target, sprite));
            var pivot = new Vector2(bounds.Center.X, bounds.Bottom); // pivot on bottom center;
            var pivotTranslation = Matrix3x2.CreateTranslation(-pivot);
            var pivotTranslationBack = Matrix3x2.CreateTranslation(pivot);
            var skew = Matrix3x2.CreateSkew(MathF.Tan(angle), 0f);
            var scale = Matrix3x2.CreateScale(1f, MathF.Abs(data.Direction.Y));

            var matty = pivotTranslation * scale * skew * pivotTranslationBack * prevMatty;
            sprite.LocalMatrix = matty;

            var alpha = 1f - data.Strength;
            var color = ShadowData.Color.WithAlpha(prevColor.A * alpha);
            _sprite.SetColor((target, sprite), color);

            _sprite.RenderSprite((target, sprite), args.WorldHandle, eyeRot, worldRot, worldPos);

            _sprite.SetColor((target, sprite), prevColor);
            sprite.LocalMatrix = prevMatty;
        }

        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }

    private static ShadowData? GetInterpulatedShadow(Entity<TransformComponent> ent, Entity<ShadowGridComponent> grid)
    {
        var x0 = (int)MathF.Floor(ent.Comp.LocalPosition.X);
        var y0 = (int)MathF.Floor(ent.Comp.LocalPosition.Y);
        var x1 = x0 + 1;
        var y1 = y0 + 1;

        var fx = ent.Comp.LocalPosition.X - x0;
        var fy = ent.Comp.LocalPosition.Y - y0;

        var has00 = grid.Comp.ShadowMap.TryGetValue(new(x0, y0), out var s00);
        var has10 = grid.Comp.ShadowMap.TryGetValue(new(x1, y0), out var s10);
        var has01 = grid.Comp.ShadowMap.TryGetValue(new(x0, y1), out var s01);
        var has11 = grid.Comp.ShadowMap.TryGetValue(new(x1, y1), out var s11);

        if (!has00 && !has10 && !has01 && !has11)
            return null;

        if (!has00)
            s00 = ShadowData.Empty;
        if (!has10)
            s10 = ShadowData.Empty;
        if (!has01)
            s01 = ShadowData.Empty;
        if (!has11)
            s11 = ShadowData.Empty;

        var sx0 = ShadowData.Lerp(s00, s10, fx);
        var sx1 = ShadowData.Lerp(s01, s11, fx);
        var data = ShadowData.Lerp(sx0, sx1, fy);

        if (data.Strength < ShadowData.MinStrength)
            return null;

        return data;
    }

    public void AddEntity(EntityUid toAdd)
    {
        _toRender.Add(toAdd);
    }

    public void RemoveEntity(EntityUid toRemove)
    {
        _toRender.Remove(toRemove);
    }
}
