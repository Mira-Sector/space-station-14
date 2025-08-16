using Content.Client.Parallax;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.StationEvents;

public sealed class SolarFlareOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    private const float OffsetMinMagnitude = 1.414213562f;
    private const float OffsetMaxMagnitude = 1.5f;

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly ShaderInstance _shader;


    public SolarFlareOverlay() : base()
    {
        IoCManager.InjectDependencies(this);

        ZIndex = ParallaxSystem.ParallaxZIndex + 1;

        _shader = _prototype.Index<ShaderPrototype>("SolarFlare").InstanceUnique();
        _shader.SetParameter("seed", _random.NextFloat());
        _shader.SetParameter("offset", _random.NextVector2(OffsetMinMagnitude, OffsetMaxMagnitude));
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return;

        args.WorldHandle.UseShader(_shader);
        args.WorldHandle.DrawRect(args.WorldBounds, Color.White);
        args.WorldHandle.UseShader(null);
    }
}
