using Content.Server.GameTicking.Rules;
using Content.Server.Objectives.Components;
using Content.Server.Obsessed;
using Content.Server.Roles;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Systems;

public sealed class HugObsessionConditionSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ObsessedRuleSystem _obsession = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HugObsessionConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<HugObsessionConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssigned);
        SubscribeLocalEvent<HugObsessionConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        SubscribeLocalEvent<ObsessionComponent, InteractionSuccessEvent>(OnInteractHand);
    }

    private void OnAssigned(EntityUid uid, HugObsessionConditionComponent comp, ref ObjectiveAssignedEvent args)
    {
        _obsession.PickObsession(args.MindId);

        if (!GetTarget(args.MindId, out var roleComp) ||
            roleComp == null ||
            roleComp.Obsession == null)
        {
            args.Cancelled = true;
            return;
        }

        if (comp.HugsNeeded != null)
            return;

        comp.HugsNeeded = (uint) _random.Next((int)comp.Min, (int)comp.Max);
    }

    private void OnAfterAssigned(EntityUid uid, HugObsessionConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        if (!GetTarget(args.MindId, out var roleComp) ||
            roleComp == null ||
            roleComp.Obsession == null ||
            comp.HugsNeeded == null)
        {
            return;
        }

        var targetName = "Unknown";
        if (TryComp<MindComponent>(roleComp.Obsession, out var mind) && mind.CharacterName != null)
        {
            targetName = mind.CharacterName;
        }

        var title = Loc.GetString(comp.Title, ("targetName", targetName), ("count", comp.HugsNeeded));

        _metaData.SetEntityName(uid, title, args.Meta);

        if (comp.Description != null)
            _metaData.SetEntityDescription(uid, Loc.GetString(comp.Description), args.Meta);
    }

    private void OnInteractHand(EntityUid uid, ObsessionComponent _, InteractionSuccessEvent args)
    {
        AddHug(args.User, uid);
    }

    public void AddHug(EntityUid hugger, EntityUid hugged)
    {
        if (!TryComp<MindContainerComponent>(hugger, out var containerComp) ||
            containerComp.Mind == null)
            return;

        var huggerMind = containerComp.Mind.Value;

        if (!TryComp<MindComponent>(huggerMind, out var mindComp))
            return;

        foreach (var objective in mindComp.Objectives)
        {
            if (!TryComp<HugObsessionConditionComponent>(objective, out var hugComp))
                continue;

            if (!TryComp<ObsessedRoleComponent>(huggerMind, out var roleComp) ||
                roleComp.Obsession == null)
                continue;

            if (!TryComp<MindContainerComponent>(hugged, out var huggedMind) ||
                huggedMind.Mind == null)
                continue;

            if (roleComp.Obsession != huggedMind.Mind)
                continue;

            hugComp.HugsPerformed += 1;
        }
    }

    private void OnGetProgress(EntityUid uid, HugObsessionConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(comp);
    }

    private float GetProgress(HugObsessionConditionComponent comp)
    {
        if (comp.HugsNeeded == null)
            return 1f;

        if (comp.HugsNeeded <= 0)
            return 1f;

        var progress = (float) comp.HugsPerformed / (float) comp.HugsNeeded;

        if (progress < 0)
            return 1f;

        return progress;
    }

    private bool GetTarget(EntityUid mind, out ObsessedRoleComponent? roleComp)
    {
        roleComp = null;

        if (!TryComp<ObsessedRoleComponent>(mind, out roleComp) ||
            roleComp == null)
            return false;

        return true;
    }
}
