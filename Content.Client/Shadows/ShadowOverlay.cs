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
        IoCManager.InjectDependencies(this);
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

            var indices = new Vector2i((int)MathF.Round(xform.LocalPosition.X), (int)MathF.Round(xform.LocalPosition.Y));
            if (!grid.ShadowMap.TryGetValue(indices, out var data))
                continue;

            var eyeRot = args.Viewport.Eye?.Rotation ?? Angle.Zero;
            var worldPos = _xform.GetWorldPosition(xform);
            var worldRot = _xform.GetWorldRotation(xform);

            var angle = MathF.Atan2(data.Direction.Y, data.Direction.X);

            var skew = Matrix3x2.CreateSkew(MathF.Tan(angle), 0f);
            args.WorldHandle.SetTransform(skew);
            _sprite.RenderSprite((target, sprite), args.WorldHandle, eyeRot, worldRot, worldPos);
        }

        args.WorldHandle.SetTransform(Matrix3x2.Identity);
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
