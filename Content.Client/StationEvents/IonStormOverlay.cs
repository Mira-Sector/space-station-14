using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.StationEvents;

public sealed class IonStormOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public HashSet<MapId> Maps = [];

    private readonly ShaderInstance _shader;

    public IonStormOverlay() : base()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 128;

        _shader = _prototype.Index<ShaderPrototype>("IonStorm").InstanceUnique();
        _shader.SetParameter("seed", _random.NextFloat());
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return Maps.Contains(args.MapId);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
        args.WorldHandle.UseShader(_shader);
        args.WorldHandle.DrawRect(args.WorldBounds, Color.White);
        args.WorldHandle.UseShader(null);
    }
}
