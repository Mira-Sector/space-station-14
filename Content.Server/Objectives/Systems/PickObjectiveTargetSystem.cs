using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Revolutionary.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles assinging a target to an objective entity with <see cref="TargetObjectiveComponent"/> using different components.
/// These can be combined with condition components for objective completions in order to create a variety of objectives.
/// </summary>
public sealed class PickObjectiveTargetSystem : EntitySystem
{
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ObsessedRuleSystem _obsession = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PickSpecificPersonComponent, ObjectiveAssignedEvent>(OnSpecificPersonAssigned);
        SubscribeLocalEvent<PickRandomPersonComponent, ObjectiveAssignedEvent>(OnRandomPersonAssigned);
        SubscribeLocalEvent<PickRandomHeadComponent, ObjectiveAssignedEvent>(OnRandomHeadAssigned);

        SubscribeLocalEvent<RandomTraitorProgressComponent, ObjectiveAssignedEvent>(OnRandomTraitorProgressAssigned);
        SubscribeLocalEvent<RandomTraitorAliveComponent, ObjectiveAssignedEvent>(OnRandomTraitorAliveAssigned);

        SubscribeLocalEvent<PickObsessionComponent, ObjectiveAssignedEvent>(OnObsessionAssigned);
        SubscribeLocalEvent<PickObsessionDepartmentComponent, ObjectiveAssignedEvent>(OnObsessionDepartmentAssigned);
        SubscribeLocalEvent<PickObsessionDepartmentComponent, ObjectiveAfterAssignEvent>(OnAfterObsessionDepartmentAssigned);
    }

    private void OnSpecificPersonAssigned(Entity<PickSpecificPersonComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid objective prototype
        if (!TryComp<TargetObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        if (args.Mind.OwnedEntity == null)
        {
            args.Cancelled = true;
            return;
        }

        var user = args.Mind.OwnedEntity.Value;
        if (!TryComp<TargetOverrideComponent>(user, out var targetComp) || targetComp.Target == null)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent.Owner, targetComp.Target.Value);
    }

    private void OnRandomPersonAssigned(Entity<PickRandomPersonComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid objective prototype
        if (!TryComp<TargetObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        var allHumans = _mind.GetAliveHumans(args.MindId);

        // Can't have multiple objectives to kill the same person
        foreach (var objective in args.Mind.Objectives)
        {
            if (HasComp<KillPersonConditionComponent>(objective) && TryComp<TargetObjectiveComponent>(objective, out var kill))
            {
                allHumans.RemoveWhere(x => x.Owner == kill.Target);
            }
        }

        // no other humans to kill
        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent.Owner, _random.Pick(allHumans), target);
    }

    private void OnRandomHeadAssigned(Entity<PickRandomHeadComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<TargetObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        // no other humans to kill
        var allHumans = _mind.GetAliveHumans(args.MindId);
        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        var allHeads = new HashSet<Entity<MindComponent>>();
        foreach (var person in allHumans)
        {
            if (TryComp<MindComponent>(person, out var mind) && mind.OwnedEntity is { } owned && HasComp<CommandStaffComponent>(owned))
                allHeads.Add(person);
        }

        if (allHeads.Count == 0)
            allHeads = allHumans; // fallback to non-head target

        _target.SetTarget(ent.Owner, _random.Pick(allHeads), target);
    }

    private void OnRandomTraitorProgressAssigned(Entity<RandomTraitorProgressComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<TargetObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }

        var traitors = _traitorRule.GetOtherTraitorMindsAliveAndConnected(args.Mind).ToHashSet();

        // cant help anyone who is tasked with helping:
        // 1. thats boring
        // 2. no cyclic progress dependencies!!!
        foreach (var traitor in traitors)
        {
            // TODO: replace this with TryComp<ObjectivesComponent>(traitor) or something when objectives are moved out of mind
            if (!TryComp<MindComponent>(traitor.Id, out var mind))
                continue;

            foreach (var objective in mind.Objectives)
            {
                if (HasComp<HelpProgressConditionComponent>(objective))
                    traitors.RemoveWhere(x => x.Mind == mind);
            }
        }

        // Can't have multiple objectives to help/save the same person
        foreach (var objective in args.Mind.Objectives)
        {
            if (HasComp<RandomTraitorAliveComponent>(objective) || HasComp<RandomTraitorProgressComponent>(objective))
            {
                if (TryComp<TargetObjectiveComponent>(objective, out var help))
                {
                    traitors.RemoveWhere(x => x.Id == help.Target);
                }
            }
        }

        // no more helpable traitors
        if (traitors.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent.Owner, _random.Pick(traitors).Id, target);
    }

    private void OnRandomTraitorAliveAssigned(Entity<RandomTraitorAliveComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<TargetObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }

        var traitors = _traitorRule.GetOtherTraitorMindsAliveAndConnected(args.Mind).ToHashSet();

        // Can't have multiple objectives to help/save the same person
        foreach (var objective in args.Mind.Objectives)
        {
            if (HasComp<RandomTraitorAliveComponent>(objective) || HasComp<RandomTraitorProgressComponent>(objective))
            {
                if (TryComp<TargetObjectiveComponent>(objective, out var help))
                {
                    traitors.RemoveWhere(x => x.Id == help.Target);
                }
            }
        }

        // You are the first/only traitor.
        if (traitors.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent.Owner, _random.Pick(traitors).Id, target);
    }

    private void OnObsessionAssigned(Entity<PickObsessionComponent> ent, ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<TargetObjectiveComponent>(ent, out var target))
        {
            args.Cancelled = true;
            return;
        }

        if (target.Target != null)
            return;

        if (!_obsession.TryGetRole(args.Mind, out var roleComp))
            return;

        _obsession.PickObsession(args.MindId, roleComp, args.Mind);

        if (roleComp.Obsession == null)
            return;

        _target.SetTarget(ent, roleComp.Obsession.Value, target);
    }

    private void OnObsessionDepartmentAssigned(Entity<PickObsessionDepartmentComponent> ent, ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<TargetObjectiveComponent>(ent, out var target))
        {
            args.Cancelled = true;
            return;
        }

        if (target.Targets != null)
            return;

        if (!_obsession.TryGetRole(args.Mind, out var roleComp))
            return;

        _obsession.PickObsession(args.MindId, roleComp, args.Mind);

        if (roleComp.Obsession == null)
        {
            args.Cancelled = true;
            return;
        }

        if (!_job.MindTryGetJob(roleComp.Obsession, out var obsessionJob))
        {
            args.Cancelled = true;
            return;
        }

        // get the obsessions department
        DepartmentPrototype? obsessionDepartment = null;
        foreach (var department in _prototype.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (department.Roles.Contains(obsessionJob))
            {
                obsessionDepartment = department;
                break;
            }
        }

        if (obsessionDepartment == null)
        {
            args.Cancelled = true;
            return;
        }

        var allHumans = _mind.GetAliveHumans(roleComp.Obsession.Value);
        allHumans.Remove((args.MindId, args.Mind)); //dont want the player to have a suicide mission

        // get everyone in the obsessions department
        List<EntityUid> targets = new();
        foreach (var human in allHumans)
        {
            if (!_job.MindTryGetJob(human, out var job))
                continue;

            if (obsessionDepartment.Roles.Contains(job))
                targets.Add(human);
        }

        if (targets.Count <= 0)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTargets(ent, targets, target);
    }

    private void OnAfterObsessionDepartmentAssigned(Entity<PickObsessionDepartmentComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (!_obsession.TryGetRole(args.Mind, out var roleComp))
            return;

        if (roleComp.Obsession == null)
            return;

        var targetName = string.Empty;
        if (TryComp<MindComponent>(roleComp.Obsession, out var mind) && mind.CharacterName != null)
        {
            targetName = mind.CharacterName;
        }

        var title = Loc.GetString(ent.Comp.Title, ("targetName", targetName));

        _metaData.SetEntityName(ent, title, args.Meta);

        if (ent.Comp.Description != null)
            _metaData.SetEntityDescription(ent, Loc.GetString(ent.Comp.Description), args.Meta);
    }

}
