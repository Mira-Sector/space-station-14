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

    private readonly SharedTransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    public Dictionary<Entity<ShowXRayComponent>, HashSet<EntityUid>> Entities = new();

    public XRayOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transform = _entityManager.System<SharedTransformSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return Entities.Any();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var worldHandle = args.WorldHandle;

        foreach (var (xray, entities) in Entities)
        {
            var shader = _prototype.Index<ShaderPrototype>(xray.Comp.Shader).InstanceUnique();

            foreach (var entity in entities)
            {
                if (!_entityManager.TryGetComponent<SpriteComponent>(entity, out var sprite))
                    continue;

                var oldShader = sprite.PostShader;
                sprite.PostShader = shader;

                var (entPos, entRot) = _transform.GetWorldPositionRotation(entity);

                var rot = args.Viewport.Eye?.Rotation ?? default;
                sprite.Render(worldHandle, rot, entRot, null, entPos);

                sprite.PostShader = oldShader;
            }
        }

        worldHandle.UseShader(null);
        worldHandle.SetTransform(Matrix3x2.Identity);
    }
}
