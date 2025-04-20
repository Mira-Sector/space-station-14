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
        foreach (var (xray, entities) in Entities)
        {
            var shader = _prototype.Index<ShaderPrototype>(xray.Comp.Shader).InstanceUnique();
            args.WorldHandle.UseShader(shader);

            foreach (var entity in entities)
            {
                if (!_entityManager.TryGetComponent<SpriteComponent>(entity, out var sprite))
                    continue;

                var entityXform = _entityManager.GetComponent<TransformComponent>(entity);
                var (entPos, entRot, worldMatrix) = _transform.GetWorldPositionRotationMatrix(entityXform);

                var eyeRot = args.Viewport.Eye?.Rotation ?? default;
                var spriteRot = eyeRot - entRot;

                var entityMatrix = Matrix3Helpers.CreateTransform(entPos, spriteRot);
                args.DrawingHandle.SetTransform(worldMatrix);

                var layerRot = sprite.NoRotation || sprite.SnapCardinals ? eyeRot : spriteRot;

                foreach (var spriteLayer in sprite.AllLayers)
                {
                    if (spriteLayer is not SpriteComponent.Layer layer || !_sprite.IsVisible(layer))
                        continue;

                    if (layer.ActualRsi is not { } rsi || !rsi.TryGetState(layer.State, out var rsiState))
                        continue;

                    var dir = SpriteComponent.Layer.GetDirection(rsiState.RsiDirections, spriteRot);
                    var texture = rsiState.GetFrame(dir, 0);

                    var textureSize = texture.Size / (float)EyeManager.PixelsPerMeter;
                    var quad = Box2.FromDimensions(textureSize / -2, textureSize);
                    var quadRotated = new Box2Rotated(quad, layerRot);

                    args.WorldHandle.DrawTextureRect(texture, quadRotated);
                }
            }
        }

        args.DrawingHandle.SetTransform(Matrix3x2.Identity);
        args.WorldHandle.UseShader(null);
    }
}
