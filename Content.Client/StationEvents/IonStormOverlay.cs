using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client.StationEvents;

public sealed class IonStormOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly SpriteSystem _sprite;

    public HashSet<MapId> Maps = [];

    private readonly ShaderInstance _shader;
    private readonly Texture _noiseTexture;

    private static readonly ResPath NoiseTexturePath = new("/Textures/Parallaxes/noise.png");

    public IonStormOverlay() : base()
    {
        IoCManager.InjectDependencies(this);
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
        args.WorldHandle.DrawTextureRect(_noiseTexture, args.WorldBounds);
        args.WorldHandle.UseShader(null);
    }

    public void NewDirection()
    {
        _shader.SetParameter("direction", _random.NextVector2().Normalized());
    }
}
