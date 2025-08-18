using Content.Client.Parallax;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client.StationEvents;

public sealed class IonStormOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly ParallaxSystem _parallax;
    private readonly SpriteSystem _sprite;

    public HashSet<MapId> Maps = [];

    private readonly ShaderInstance _shader;
    private readonly Texture _noiseTexture;

    private static readonly ResPath NoiseTexturePath = new("/Textures/Parallaxes/noise.png");
    private const float NoiseTextureScale = 0.5f;

    private Vector2 _direction;
    private float _speed;

    private const float MinSpeed = 1f;
    private const float MaxSpeed = 2.5f;

    public IonStormOverlay() : base()
    {
        IoCManager.InjectDependencies(this);
        _parallax = _entity.System<ParallaxSystem>();
        _sprite = _entity.System<SpriteSystem>();

        ZIndex = 128;

        var sprite = new SpriteSpecifier.Texture(NoiseTexturePath);
        _noiseTexture = _sprite.Frame0(sprite);

        _shader = _prototype.Index<ShaderPrototype>("IonStorm").InstanceUnique();
        _shader.SetParameter("seed", _random.NextFloat());
        NewDirection();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return Maps.Contains(args.MapId);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
        args.WorldHandle.UseShader(null);

        _shader.SetParameter("noise", _noiseTexture);
        args.WorldHandle.UseShader(_shader);
        _parallax.DrawParallax(args.WorldHandle, args.WorldAABB, _noiseTexture, _timing.RealTime, Vector2.Zero, _direction * _speed, NoiseTextureScale);
        args.WorldHandle.UseShader(null);
    }

    public void NewDirection()
    {
        _speed = _random.NextFloat(MinSpeed, MaxSpeed);
        _direction = _random.NextVector2().Normalized();
        _shader.SetParameter("direction", _direction);
    }
}
