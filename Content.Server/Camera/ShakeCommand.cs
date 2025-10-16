using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Camera;
using Content.Shared.Localizations;
using Robust.Shared.Console;
using System.Numerics;

namespace Content.Server.Camera;

[AdminCommand(AdminFlags.Fun)]
public sealed partial class ShakeCommand : LocalizedEntityCommands
{
    [Dependency] private readonly CameraShakeSystem _shake = default!;

    public override string Command => "shake";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 7)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-arguments", ("minimum", 7)));
            return;
        }

        if (!float.TryParse(args[0], out var x) || !float.TryParse(args[1], out var y))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[1])));
            return;
        }

        var dir = new Vector2(x, y).Normalized();

        if (!float.TryParse(args[2], out var minMagnitude))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[2])));
            return;
        }

        if (!float.TryParse(args[3], out var maxMagnitude))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[3])));
            return;
        }

        if (!float.TryParse(args[4], out var noiseWeight))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[4])));
            return;
        }
        else if (noiseWeight > 1 || noiseWeight < 0)
        {
            shell.WriteError(Loc.GetString("shell-argument-number-must-be-between", ("index", args[4]), ("lower", 0), ("upper", 1)));
            return;
        }

        if (!TimeSpan.TryParseExact(args[5], ContentLocalizationManager.TimeSpanMinutesFormats, LocalizationManager.DefaultCulture, out var duration))
        {
            shell.WriteError(Loc.GetString("shell-timespan-minutes-must-be-correct", ("span", args[5])));
            return;
        }

        if (!float.TryParse(args[6], out var frequency))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[6])));
            return;
        }

        if (args.Length == 7)
        {
            if (shell.Player?.AttachedEntity is not { } local || !EntityManager.EntityExists(local))
            {
                shell.WriteError(Loc.GetString("cmd-failure-no-attached-entity"));
                return;
            }

            _shake.ShakeCamera(local, dir, minMagnitude, maxMagnitude, noiseWeight, duration, frequency);
            return;
        }

        for (var i = 7; i < args.Length; i++)
        {
            var arg = args[i];
            if (!NetEntity.TryParse(arg, out var netEnt) || !EntityManager.TryGetEntity(netEnt, out var uid) || !EntityManager.EntityExists(uid))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-uid", ("arg", arg)));
                continue;
            }

            _shake.ShakeCamera(uid.Value, dir, minMagnitude, maxMagnitude, noiseWeight, duration, frequency);
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint(Loc.GetString("cmd-shake-x")),
            2 => CompletionResult.FromHint(Loc.GetString("cmd-shake-y")),
            3 => CompletionResult.FromHint(Loc.GetString("cmd-shake-min-magnitude")),
            4 => CompletionResult.FromHint(Loc.GetString("cmd-shake-max-magnitude")),
            5 => CompletionResult.FromHint(Loc.GetString("cmd-shake-noise")),
            6 => CompletionResult.FromHint(Loc.GetString("cmd-shake-duration")),
            7 => CompletionResult.FromHint(Loc.GetString("cmd-shake-frequency")),
            _ => CompletionResult.FromHintOptions(CompletionHelper.NetEntities(args[^1]), Loc.GetString("cmd-shake-uid")),
        };
    }
}
