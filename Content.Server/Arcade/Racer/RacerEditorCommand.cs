using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.Arcade.Racer;

[AdminCommand(AdminFlags.Mapping)]
public sealed class RacerEditorCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IResourceManager _resource = default!;
    [Dependency] private readonly RacerArcadeSystem _racer = default!;

    public override string Command => "racereditor";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromOptions(CompletionHelper.UserFilePath(args[0], _resource.UserData)
                        .Concat(CompletionHelper.ContentFilePath(args[0], _resource))),
            _ => CompletionResult.Empty
        };
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
            return;

        if (shell.Player is not { } player)
            return;

        _racer.StartEditingSession(player, new ResPath(args[0]));
    }
}
