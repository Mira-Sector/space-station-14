using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.StationEvents;

public sealed class SolarFlareOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
    public override bool RequestScreenTexture => true;

    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly ShaderInstance _shader;

    public SolarFlareOverlay() : base()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototype.Index<ShaderPrototype>("SolarFlare").InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
