using Content.Server.GameTicking.Rules.Components;
using Content.Server.Obsessed;
using Content.Server.Roles;
using Content.Shared.Mind;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.GameTicking.Rules;

public sealed class ObsessedRuleSystem : GameRuleSystem<ObsessedRuleComponent>
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public bool TryGetRole(MindComponent mind, [NotNullWhen(true)] out ObsessedRoleComponent? role)
    {
        role = null;

        foreach (var mindRole in mind.MindRoles)
        {
            if (TryComp<ObsessedRoleComponent>(mindRole, out role))
                return true;
        }

        return false;
    }

    public void PickObsession(EntityUid mind, MindComponent mindComp)
    {
        if (!TryGetRole(mindComp, out var component))
            return;

        PickObsession(mind, component, mindComp);
    }

    public void PickObsession(EntityUid mind, ObsessedRoleComponent component, MindComponent? mindComp = null)
    {
        if (!Resolve(mind, ref mindComp))
            return;

        if (component.Obsession != null)
            return;

        var minds = _mind.GetAliveHumans(mind);
        minds.Remove((mind, mindComp));

        if (minds.Count <= 0)
        {
            RemComp<ObsessedRoleComponent>(mind);
            return;
        }

        var obsessionMind = _random.Pick(minds);
        component.Obsession = obsessionMind;

        if (!TryComp<MindComponent>(obsessionMind, out var obsessionMindComp) || obsessionMindComp?.CurrentEntity == null)
            return;

        EnsureComp<ObsessionComponent>(obsessionMind);
        EnsureComp<ObsessionComponent>(obsessionMindComp.CurrentEntity.Value);
    }
}
