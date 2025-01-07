using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Silicons.Laws;
using Content.Server.Roles;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed class MalfunctionRuleSystem : GameRuleSystem<MalfunctionRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SiliconLawSystem _laws = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MalfunctionRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);
        SubscribeLocalEvent<MalfunctionRoleComponent, GetSiliconLawsEvent>(GetLaws);
    }

    private void AfterAntagSelected(EntityUid uid, MalfunctionRuleComponent rule, ref AfterAntagEntitySelectedEvent args)
    {
        if (!TryComp<SiliconLawProviderComponent>(args.EntityUid, out var lawsComp))
            return;

        if (lawsComp.Lawset == null)
            _laws.SetLaws(_laws.GetLawset(lawsComp.Laws).Laws, args.EntityUid);

        if (lawsComp.Lawset == null)
            return;

        var exitingLaws = lawsComp.Lawset.Laws;
        List<SiliconLaw> newLaws = new();
        List<SiliconLaw> laws = new();

        foreach (var lawId in rule.Laws)
        {
            if (!_prototype.TryIndex(lawId, out var law))
                continue;

            laws.Add(law);
            newLaws.Add(law);
        }

        foreach (var existingLaw in exitingLaws)
        {
            foreach (var law in laws)
            {
                if (law.Order == existingLaw.Order)
                    continue;

                newLaws.Add(existingLaw);
                break;
            }
        }

        _laws.SetLaws(newLaws, args.EntityUid);

        if (!_mind.TryGetMind(args.EntityUid, out var mindUid, out var mind))
            return;

        foreach (var role in mind.MindRoles)
        {
            if (!TryComp<MalfunctionRoleComponent>(role, out var malfunction))
                continue;

            malfunction.Laws = laws;
            break;
        }
    }

    private void GetLaws(EntityUid uid, MalfunctionRoleComponent component, ref GetSiliconLawsEvent args)
    {
        foreach (var law in component.Laws)
        {
            args.PriorityLaws.Add(law);
        }

        args.Handled = true;
    }
}
