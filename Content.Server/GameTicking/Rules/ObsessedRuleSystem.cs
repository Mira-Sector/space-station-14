using Content.Server.GameTicking.Rules.Components;
using Content.Server.Obsessed;
using Content.Server.Roles;
using Content.Shared.Mind;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class ObsessedRuleSystem : GameRuleSystem<ObsessedRuleComponent>
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ObsessedRoleComponent, ComponentInit>(OnInit);
    }
    private void OnInit(EntityUid uid, ObsessedRoleComponent component, ComponentInit args)
    {
        PickObsession(uid);
    }

    public void PickObsession(EntityUid mind, ObsessedRoleComponent? component = null)
    {
        if (!Resolve(mind, ref component))
            return;

        if (component.Obsession != null)
            return;

        var obsessionMind = _random.Pick(_mind.GetAliveHumansExcept(mind));
        component.Obsession = obsessionMind;

        if (!TryComp<MindComponent>(obsessionMind, out var mindComp) ||
            mindComp == null ||
            mindComp.CurrentEntity == null)
            return;

        EnsureComp<ObsessionComponent>(obsessionMind);
        EnsureComp<ObsessionComponent>(mindComp.CurrentEntity.Value);
    }
}
