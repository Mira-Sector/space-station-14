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
        if (!_obsession.TryGetRole(args.Mind, out var roleComp))
            return;

        _obsession.PickObsession(args.MindId, roleComp, args.Mind);

        if (roleComp.Obsession == null)
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
        if (!_obsession.TryGetRole(args.Mind, out var roleComp))
            return;

        if (roleComp.Obsession == null ||
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
        if (!TryComp<MindContainerComponent>(hugger, out var huggerContainerComp) ||
            huggerContainerComp.Mind == null)
            return;

        if (!TryComp<MindContainerComponent>(hugged, out var huggedContainerComp) ||
            huggedContainerComp.Mind == null)
            return;

        var huggerMind = huggerContainerComp.Mind.Value;
        var huggedMind = huggedContainerComp.Mind.Value;

        if (!TryComp<MindComponent>(huggerMind, out var huggerMindComp))
            return;

        foreach (var objective in huggerMindComp.Objectives)
        {
            if (!TryComp<HugObsessionConditionComponent>(objective, out var hugComp))
                continue;

            if (!_obsession.TryGetRole(huggerMindComp, out var roleComp))
                continue;

            if (roleComp.Obsession != huggedMind)
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
}
