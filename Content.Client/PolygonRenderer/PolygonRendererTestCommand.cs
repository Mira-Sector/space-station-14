using Robust.Shared.Console;
using JetBrains.Annotations;

namespace Content.Client.PolygonRenderer;

[UsedImplicitly]
public sealed partial class PolygonRendererTestCommand : LocalizedCommands
{
    public override string Command => "polygonrenderer_test";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var control = new PolygonRendererTestWindow();
        control.OpenCentered();
    }
}
