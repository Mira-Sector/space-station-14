using Content.Server.Administration;
using Content.Server.Arcade.Racer.Systems;
using Content.Shared.Administration;
using Content.Shared.Arcade.Racer;
using Robust.Shared.Console;

namespace Content.Server.Arcade.Racer;

[AdminCommand(AdminFlags.Debug)]
public sealed class RacerDebugCommand : LocalizedEntityCommands
{
    [Dependency] private readonly RacerArcadeSystem _racer = default!;

    public override string Command => "racer_debug";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var options = GetAvailableFlags(args);
        return CompletionResult.FromOptions(options);
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
            return;

        var flags = GetFlags(args);
        _racer.SetDebugFlags(flags, player);
    }

    private static RacerArcadeDebugFlags GetFlags(IEnumerable<string> args)
    {
        var found = RacerArcadeDebugFlags.None;

        foreach (var arg in args)
        {
            if (!Enum.TryParse<RacerArcadeDebugFlags>(arg, out var flag))
                continue;

            found |= flag;
        }

        return found;
    }

    private static IEnumerable<string> GetAvailableFlags(IEnumerable<string> args)
    {
        var found = GetFlags(args);

        for (var i = (int)RacerArcadeDebugFlags.First; i <= (int)RacerArcadeDebugFlags.Last; i <<= 1)
        {
            var flag = (RacerArcadeDebugFlags)i;
            if (found.HasFlag(flag))
                continue;

            if (Enum.GetName(flag) is { } name)
                yield return name;
        }
    }
}
