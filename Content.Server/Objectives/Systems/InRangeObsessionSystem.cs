using Content.Server.GameTicking.Rules;
using Content.Server.Objectives.Components;
using Content.Shared.Physics;
using Content.Server.Roles;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Objectives.Systems;

public sealed class InRangeObsessionSystem : EntitySystem
{
    const int CheckRate = 5;
    const int Range = 10;
    const CollisionGroup CollisionMask = CollisionGroup.Opaque | CollisionGroup.GhostImpassable | CollisionGroup.BulletImpassable;

    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly ObsessedRuleSystem _obsession = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InRangeObsessionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<InRangeObsessionComponent, ObjectiveAfterAssignEvent>(OnAfterAssigned);
        SubscribeLocalEvent<InRangeObsessionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<InRangeObsessionComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_gameTiming.CurTime <= comp.NextCheck)
                continue;

            var checkRateTime = TimeSpan.FromSeconds(CheckRate);
            comp.NextCheck = _gameTiming.CurTime + checkRateTime;

            if (!TryComp<MindComponent>(comp.MindId, out var obsessedMindComp) ||
                obsessedMindComp.CurrentEntity == null)
                continue;

            var obsesserEnt = obsessedMindComp.CurrentEntity;

            if (!TryComp<ObsessedRoleComponent>(comp.MindId, out var roleComp) ||
                roleComp.Obsession == null)
                continue;

            if (!TryComp<MindComponent>(roleComp.Obsession, out var obsessionMindComp) ||
                obsessionMindComp == null)
                continue;

            if (obsessionMindComp == null ||
                obsessionMindComp.CurrentEntity == null)
                continue;

            var obsessionEnt = obsessionMindComp.CurrentEntity;


                if (_interaction.InRangeAndAccessible(obsesserEnt.Value, obsessionEnt.Value, Range, CollisionMask))
                comp.TimeSpent += checkRateTime;
        }
    }

    private void OnAssigned(EntityUid uid, InRangeObsessionComponent comp, ref ObjectiveAssignedEvent args)
    {
        _obsession.PickObsession(args.MindId);

        if (!TryComp<ObsessedRoleComponent>(args.MindId, out var roleComp) ||
            roleComp == null ||
            roleComp.Obsession == null)
        {
            args.Cancelled = true;
            return;
        }

        comp.MindId = args.MindId;
        comp.TimeNeeded = TimeSpan.FromMinutes(_random.Next((int) comp.Min, (int) comp.Max));
        comp.TimeSpent = TimeSpan.Zero;
        comp.NextCheck = _gameTiming.CurTime;
    }

    private void OnAfterAssigned(EntityUid uid, InRangeObsessionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        if (!TryComp<ObsessedRoleComponent>(args.MindId, out var roleComp) ||
            roleComp == null ||
            roleComp.Obsession == null ||
            comp.TimeNeeded == TimeSpan.Zero)
        {
            return;
        }

        var targetName = "Unknown";
        if (TryComp<MindComponent>(roleComp.Obsession, out var mind) && mind.CharacterName != null)
        {
            targetName = mind.CharacterName;
        }

        var title = Loc.GetString(comp.Title, ("targetName", targetName), ("time", comp.TimeNeeded.Minutes));

        _metaData.SetEntityName(uid, title, args.Meta);

        if (comp.Description != null)
            _metaData.SetEntityDescription(uid, Loc.GetString(comp.Description), args.Meta);
    }

    private void OnGetProgress(EntityUid uid, InRangeObsessionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(comp);
    }

    private float GetProgress(InRangeObsessionComponent comp)
    {
        var progress = comp.TimeSpent / comp.TimeNeeded;

        if (progress < 0)
            return 1f;

        return (float) progress;
    }
}
