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

    private const float MaxScaleY = 1.5f;
    private const float MaxTan = 2f;

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
        args.WorldHandle.SetTransform(Matrix3x2.Identity);

        foreach (var target in _toRender)
        {
            if (!_spriteQuery.TryComp(target, out var sprite))
                continue;

            var xform = _xformQuery.GetComponent(target);
            if (!_gridQuery.TryComp(xform.GridUid, out var grid))
                continue;

            var eye = args.Viewport.Eye!;

            var eyeRot = eye.Rotation;
            var eyeScale = args.Viewport.RenderScale * eye.Scale;

            if (GetInterpulatedShadow((target, xform), (xform.GridUid.Value, grid), eyeRot) is not { } data)
                continue;

            var worldPos = _xform.GetWorldPosition(xform);
            var worldRot = _xform.GetWorldRotation(xform);

            var angle = MathF.Atan2(data.Direction.Y, data.Direction.X) * data.Strength;

            var prevColor = sprite.Color;
            var prevMatty = sprite.LocalMatrix;

            var bounds = _sprite.GetLocalBounds((target, sprite));
            var pivot = new Vector2(bounds.Center.X, bounds.Bottom); // pivot on bottom center;
            var pivotTranslation = Matrix3x2.CreateTranslation(-pivot);
            var pivotTranslationBack = Matrix3x2.CreateTranslation(pivot);
            var tan = Math.Clamp(MathF.Tan(angle), -MaxTan, MaxTan);
            var skew = Matrix3x2.CreateSkew(tan, 0f);
            var scaleY = Math.Clamp(MathF.Abs(data.Direction.Y), 0f, MaxScaleY);
            var scale = Matrix3x2.CreateScale(1f, scaleY);

            var shadowMatrix = pivotTranslation * scale * skew * pivotTranslationBack;
            sprite.LocalMatrix = shadowMatrix * prevMatty;

            var alpha = 1f - data.Strength;
            var color = ShadowData.Color.WithAlpha(prevColor.A * alpha);
            _sprite.SetColor((target, sprite), color);

            /*
             * You may be reading this and wondering "Hey, what the fuck?"
             * Me too buddy.
             *
             * We explicitly use this drawing method as it correctly handles drawing inside of frame buffers.
             * Small problem however. Eye rotation in this method also fucking rotates the rendered sprite. Why???
             * To get around this we modify the world rotation to take into account the eye rotation so directions are
             * properly rendered.
             *
             * Fuck this shit im off to bed.
            */
            var invMatrix = args.Viewport.RenderTarget.GetWorldToLocalMatrix(eye, args.Viewport.RenderScale);
            var localPos = Vector2.Transform(worldPos, invMatrix);
            var localRot = eyeRot + worldRot;
            args.RenderHandle.DrawEntity(target, localPos, eyeScale, localRot, Angle.Zero, sprite: sprite, xform: xform, xformSystem: _xform);

            _sprite.SetColor((target, sprite), prevColor);
            sprite.LocalMatrix = prevMatty;
        }
    }

    private static ShadowData? GetInterpulatedShadow(Entity<TransformComponent> ent, Entity<ShadowGridComponent> grid, Angle eyeRot)
    {
        var x0 = (int)MathF.Floor(ent.Comp.LocalPosition.X);
        var y0 = (int)MathF.Floor(ent.Comp.LocalPosition.Y);
        var x1 = x0 + 1;
        var y1 = y0 + 1;

        var fx = ent.Comp.LocalPosition.X - x0;
        var fy = ent.Comp.LocalPosition.Y - y0;

        var corners = new (Vector2i pos, float weight)[]
        {
            (new Vector2i(x0, y0), (1 - fx) * (1 - fy)),
            (new Vector2i(x1, y0), fx * (1 - fy)),
            (new Vector2i(x0, y1), (1 - fx) * fy),
            (new Vector2i(x1, y1), fx * fy)
        };

        var sumX = 0f;
        var sumY = 0f;
        var totalStrength = 0f;

        foreach (var (pos, weight) in corners)
        {
            if (weight <= 0f)
                continue;

            if (!grid.Comp.ShadowMap.TryGetValue(pos, out var shadow))
                continue;

            // weight by both distance factor and shadow strength
            var w = shadow.Strength * weight;
            sumX += shadow.Direction.X * w;
            sumY += shadow.Direction.Y * w;
            totalStrength += w;
        }

        if (totalStrength <= 0f)
            return null;

        var dir = new Vector2(sumX, sumY);
        if (dir.LengthSquared() > ShadowData.MinDirLengthSquared)
            dir = dir.Normalized();
        else
            dir = Vector2.Zero;

        // average strength
        var strength = Math.Min(totalStrength / corners.Length, 1f);
        if (strength < ShadowData.FadeStart)
            return null;

        if (strength < ShadowData.FadeEnd)
        {
            var t = (strength - ShadowData.FadeStart) / (ShadowData.FadeEnd - ShadowData.FadeStart);
            dir *= t;
            strength *= t;
        }

        return new ShadowData(eyeRot.RotateVec(dir), strength);
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
