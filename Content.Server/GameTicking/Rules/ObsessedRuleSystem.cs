using Content.Server.GameTicking.Rules.Components;
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
        var allHumans = _mind.GetAliveHumansExcept(uid);

        if (allHumans.Count == 0)
            return;

        component.Obsession = _random.Pick(allHumans);
    }
}
