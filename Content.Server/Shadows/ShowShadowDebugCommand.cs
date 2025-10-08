using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Shadows;

[AdminCommand(AdminFlags.Debug)]
public sealed class ShowShadowDebugCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ShadowSystem _shadow = default!;

    public override string Command => $"showshadowdebug";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
            return;

        _shadow.ToggleDebugOverlay(player);
    }
}
