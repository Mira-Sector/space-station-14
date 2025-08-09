using Content.Server.Administration;
using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Body.Damage.Components;
using Content.Shared.Body.Damage.Systems;
using Content.Shared.Body.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Body.Damage;

[AdminCommand(AdminFlags.Debug)]
public sealed class BodyDamageCommand : LocalizedEntityCommands
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyDamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override string Command => "bodydamage";
    public override string Description => Loc.GetString("body-damage-command-desc");
    public override string Help => Loc.GetString("body-damage-command-help");

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {

        switch (args.Length)
        {
            case 1:
                var damageable = CompletionHelper.Components<BodyDamageableComponent>(args[0], EntityManager);
                var body = CompletionHelper.Components<BodyComponent>(args[0], EntityManager);
                return CompletionResult.FromHintOptions(body.Concat(damageable), Loc.GetString("body-damage-command-hint-1"));
            case 2:
                return CompletionResult.FromHint(Loc.GetString("body-damage-command-hint-2"));
            case 3:
                if (HasBody())
                    return CompletionResult.FromHintOptions(_prototype.EnumeratePrototypes<OrganPrototype>().Select(x => x.ID), Loc.GetString("body-damage-command-hint-3"));
                else
                    return CompletionResult.FromHintOptions(CompletionHelper.Booleans, Loc.GetString("body-damage-command-hint-4"));
            case 4:
                if (HasBody())
                    return CompletionResult.FromHintOptions(CompletionHelper.Booleans, Loc.GetString("body-damage-command-hint-4"));
                else
                    return CompletionResult.Empty;
            default:
                return CompletionResult.Empty;
        }

        bool HasBody()
        {
            if (!NetEntity.TryParse(args[0], out var netUid))
                return false;

            if (!EntityManager.TryGetEntity(netUid, out var uid))
                return false;

            if (!EntityManager.HasComponent<BodyComponent>(uid.Value))
                return false;

            return true;
        }
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2 && args.Length < 4)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!float.TryParse(args[1], out var fDamage))
        {
            shell.WriteLine(Loc.GetString("body-damage-command-error-quantity", ("arg", args[1])));
            return;
        }

        var damage = FixedPoint2.New(fDamage);

        if (!NetEntity.TryParse(args[0], out var netUid) || !EntityManager.TryGetEntity(netUid, out var uid))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-uid"));
            return;
        }

        if (EntityManager.TryGetComponent<BodyDamageableComponent>(uid, out var damageable))
        {
            var forced = IsForced(args, 2);

            DealDamage((uid.Value, damageable), damage, forced);
            return;
        }
        else if (EntityManager.TryGetComponent<BodyComponent>(uid, out var body))
        {
            var forced = IsForced(args, 3);

            if (!_prototype.HasIndex<OrganPrototype>(args[2]))
            {
                shell.WriteLine(Loc.GetString("body-damage-command-error-organ-id-invalid", ("id", args[2])));
                return;
            }

            var organs = _body.GetBodyOrganEntityComps<BodyDamageableComponent>((uid.Value, body));
            foreach (var organ in organs)
            {
                if (organ.Comp2.OrganType != args[2])
                    continue;

                DealDamage((organ.Owner, organ.Comp1), damage, forced);
                return;
            }

            shell.WriteLine(Loc.GetString("body-damage-command-error-no-organ", ("uid", EntityManager.ToPrettyString(uid)), ("id", args[4])));
            return;
        }
        else
        {
            shell.WriteLine(Loc.GetString("body-damage-command-error-no-comp", ("uid", EntityManager.ToPrettyString(uid))));
            return;
        }
    }

    private static bool IsForced(string[] args, int index)
    {
        if (args.Length <= index)
            return false;

        if (!bool.TryParse(args[index], out var forced))
            return false;

        return forced;
    }

    private void DealDamage(Entity<BodyDamageableComponent?> ent, FixedPoint2 damage, bool forced)
    {
        _damageable.ChangeDamage(ent, damage, forced);
    }
}
