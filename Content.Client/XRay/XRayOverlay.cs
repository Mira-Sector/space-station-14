using Content.Shared.XRay;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Numerics;

namespace Content.Client.XRay;

public sealed class XRayOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    public Dictionary<Entity<ShowXRayComponent>, HashSet<EntityUid>> Entities = new();

    public XRayOverlay()
    {
        IoCManager.InjectDependencies(this);
        _sprite = _entityManager.System<SpriteSystem>();
        _transform = _entityManager.System<SharedTransformSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return Entities.Any();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var eyeRot = args.Viewport.Eye?.Rotation ?? Angle.Zero;

        foreach (var (xray, entities) in Entities)
        {
            var shader = _prototype.Index<ShaderPrototype>(xray.Comp.Shader).InstanceUnique();
            args.WorldHandle.UseShader(shader);

            foreach (var entity in entities)
            {
                if (!_entityManager.TryGetComponent<SpriteComponent>(entity, out var sprite))
                    continue;

                var entityXform = _entityManager.GetComponent<TransformComponent>(entity);
                var (entPos, entRot) = _transform.GetWorldPositionRotation(entityXform);

                var relativeRot = (entRot + eyeRot).Reduced().FlipPositive();

                var transform = Matrix3Helpers.CreateTransform(entPos, entityXform.LocalRotation - eyeRot);
                args.DrawingHandle.SetTransform(transform);

                var spriteRot = entityXform.LocalRotation;
                if (sprite.SnapCardinals)
                    spriteRot = entityXform.LocalRotation.GetCardinalDir().ToAngle();
                else if (sprite.NoRotation)
                    spriteRot = Angle.Zero;

                // pray to the gods sprite rendering never changes
                foreach (var spriteLayer in sprite.AllLayers)
                {
                    if (spriteLayer is not SpriteComponent.Layer layer || !_sprite.IsVisible(layer))
                        continue;

                    if (layer.ActualRsi is not {} rsi || !rsi.TryGetState(layer.State, out var rsiState))
                        continue;

                    var dir = SpriteComponent.Layer.GetDirection(rsiState.RsiDirections, relativeRot);
                    var texture = rsiState.GetFrame(dir, layer.AnimationFrame);

                    var textureSize = texture.Size / (float)EyeManager.PixelsPerMeter;
                    var quad = Box2.FromDimensions(textureSize / -2, textureSize);
                    var quadRotated = new Box2Rotated(quad, spriteRot + layer.Rotation);

                    args.WorldHandle.DrawTextureRect(texture, quadRotated, layer.Color);
                }
            }
        }

        args.DrawingHandle.SetTransform(Matrix3x2.Identity);
        args.WorldHandle.UseShader(null);
    }
}
