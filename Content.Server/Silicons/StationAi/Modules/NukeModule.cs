using Content.Server.Nuke;
using Content.Server.RoundEnd;
using Content.Shared.Charges.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Silicons.StationAi.Modules;

namespace Content.Server.Silicons.StationAi.Modules;

public sealed class NukeModuleSystem : EntitySystem
{
    [Dependency] private readonly NukeSystem _nuke = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiCanHackComponent, StationAiNukeEvent>(OnAction);
        SubscribeLocalEvent<NukeModuleStationAiComponent, GotIntellicardedEvent>(OnIntellicarded);

        SubscribeLocalEvent<NukeModuleNukeComponent, NukeDisarmSuccessEvent>(RemoveNuke);
        SubscribeLocalEvent<NukeModuleNukeComponent, NukeExplodedEvent>(OnNukeExploded);
    }

    private void OnAction(EntityUid uid, StationAiCanHackComponent component, StationAiNukeEvent args)
    {
        if (args.Handled)
            return;

        var aiXForm = Transform(uid);
        var query = EntityQueryEnumerator<NukeComponent>();

        while (query.MoveNext(out var nukeUid, out var nukeComp))
        {
            var nukeXForm = Transform(nukeUid);

            if (aiXForm.MapUid != nukeXForm.MapUid)
                continue;

            var prevTime = nukeComp.RemainingTime;
            _nuke.SetRemainingTime(nukeUid, Math.Min(nukeComp.Timer, nukeComp.RemainingTime + args.AdditionalDelay), nukeComp);

            if (!_nuke.ArmBomb(nukeUid, nukeComp))
            {
                _nuke.SetRemainingTime(nukeUid, prevTime, nukeComp);
                continue;
            }

            EnsureComp<NukeModuleNukeComponent>(nukeUid).StationAi = args.Performer;
            EnsureComp<NukeModuleStationAiComponent>(args.Performer).Nukes.Add(nukeUid);

            args.Handled |= true;
        }

        // dont let them waste their 130 power
        if (!args.Handled)
            return;

        if (TryComp<LimitedChargesComponent>(args.Action.Owner, out var charges) && charges.LastCharges > 0)
            return;

        EntityManager.DeleteEntity(args.Action);
    }

    private void RemoveNuke(EntityUid uid, NukeModuleNukeComponent component, NukeDisarmSuccessEvent args)
    {
        if (TryComp<NukeModuleStationAiComponent>(component.StationAi, out var aiComp))
            aiComp.Nukes.Remove(uid);

        RemCompDeferred(uid, component);
    }

    private void OnIntellicarded(EntityUid uid, NukeModuleStationAiComponent component, GotIntellicardedEvent args)
    {
        foreach (var nuke in component.Nukes)
        {
            _nuke.DisarmBomb(nuke);
            RemComp<NukeModuleNukeComponent>(nuke);
        }

        RemCompDeferred(uid, component);
    }

    private void OnNukeExploded(EntityUid uid, NukeModuleNukeComponent component, NukeExplodedEvent ev)
    {
        if (ev.OwningStation is not {} nukeStation)
            return;

        if (Transform(component.StationAi).GridUid != nukeStation)
            return;

        _roundEnd.EndRound();
    }
}
