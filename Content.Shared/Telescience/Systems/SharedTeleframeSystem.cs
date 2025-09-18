using Content.Shared.Telescience.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Administration.Logs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Spawners;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Shared.Telescience.Systems;

public record struct BeforeTeleportEvent(EntityUid Teleframe, bool Cancelled = false);

public record struct AfterTeleportEvent(EntityUid To, EntityUid From);

public record struct TeleportIncidentEvent(float IncidentMult);

public record struct TeleframeConsoleSpeak(string Message, bool Radio, bool Voice);

public abstract class SharedTeleframeSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private EntityQuery<PhysicsComponent> _physicsQuery; // declare the variable for the query

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleframeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TeleframeComponent, GotEmaggedEvent>(OnTeleframeEmagged);

        SubscribeLocalEvent<TeleframeConsoleComponent, TeleframeActivateMessage>(OnTeleportStart);
        SubscribeLocalEvent<TeleframeConsoleComponent, TeleframeActivateBeaconMessage>(OnTeleportBeaconStart);
        SubscribeLocalEvent<TeleframeConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<TeleframeConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<TeleframeConsoleComponent, GotEmaggedEvent>(OnConsoleEmagged);

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
    }

    public override void Update(float frameTime) // need to actually make this work
    {
        base.Update(frameTime);

        var queryCharge = EntityQueryEnumerator<TeleframeChargingComponent, TeleframeComponent>();
        while (queryCharge.MoveNext(out var uid, out var charge, out var teleframe))
        {
            if (Timing.CurTime < charge.EndTime)
                continue;
            EndTeleportCharge((uid, teleframe), (uid, charge));
        }

        var queryRecharge = EntityQueryEnumerator<TeleframeRechargingComponent, TeleframeComponent>();
        while (queryRecharge.MoveNext(out var uid, out var recharge, out var teleframe))
        {
            if (Timing.CurTime < recharge.EndTime)
                continue;
            EndTeleportRecharge((uid, teleframe), (uid, recharge));
        }

    }

    public void EndTeleportCharge(Entity<TeleframeComponent> ent, Entity<TeleframeChargingComponent> charge)
    {
        if (Exists(ent.Comp.TeleportFrom) && Exists(ent.Comp.TeleportTo)) //final check that these two exist to teleport from and to
        {
            charge.Comp.TeleportSuccess = false;
            charge.Comp.FailReason = "collapse";
        }

        if (charge.Comp.TeleportSuccess == true) //if teleport is still good to go, engage
            OnTeleport(ent); //teleport
        else
            TeleportFail(ent, charge.Comp.FailReason);

        if (charge.Comp.WillExplode == true) //and afterwards, if the Teleframe should explode, it does.
            Log.Debug("explode");

        RemCompDeferred<TeleframeChargingComponent>(ent); //stop charging
        var rechargeComp = AddComp<TeleframeRechargingComponent>(ent); //start recharging
        rechargeComp.Duration = ent.Comp.RechargeDuration;
        rechargeComp.EndTime = ent.Comp.RechargeDuration + Timing.CurTime;
    }

    ///<summary>
    /// Teleportation has failed, clean up teleportation entities
    /// also summon some l̶i̶g̶h̶t̶n̶i̶n̶g̶ smoke, for fun.
    /// </summary>
    public void TeleportFail(Entity<TeleframeComponent> ent, string failReason)
    {
        EntityManager.PredictedQueueDeleteEntity(ent.Comp.TeleportFrom);
        EntityManager.PredictedQueueDeleteEntity(ent.Comp.TeleportTo);

        var pos = Transform(ent).Coordinates;
        SpawnAtPosition("EffectFlashBluespace", pos); //flash
        SpawnAtPosition("WizardSmoke", pos); //and a pop of smoke

        if (ent.Comp.LinkedConsole != null) //raise event to have console say what the error is
            RaiseLocalEvent(ent.Comp.LinkedConsole ?? EntityUid.Invalid, new TeleframeConsoleSpeak(
                Loc.GetString("teleport-fail", ("reason", Loc.GetString("teleport-fail-" + failReason))),
                false, true));
    }

    public void EndTeleportRecharge(Entity<TeleframeComponent> ent, Entity<TeleframeRechargingComponent> charge)
    {
        ent.Comp.ReadyToTeleport = true;
        RemCompDeferred<TeleframeRechargingComponent>(ent);
    }

    public void OnTeleportStart(Entity<TeleframeConsoleComponent> ent, ref TeleframeActivateMessage args)
    {
        if (!TryGetEntity(ent.Comp.LinkedTeleframe, out var teleNetEnt) || !TryComp<TeleframeComponent>(teleNetEnt, out var teleComp))
            return; //if no linked Teleframe, can't teleport.

        //if (!_timing.IsFirstTimePredicted) //prevent it getting spammed
        //    return;

        var teleEnt = teleNetEnt ?? EntityUid.Invalid; //de-nullable teleNetEnt to prevent RaiseLocalEvent getting upset.
        var tp = Transform(teleEnt); //get transform of the Teleframe for MapID
        teleComp.Target = new MapCoordinates(args.Coords, tp.MapID); //coordinates of target, need to be able to replace MapId for beacons
        teleComp.TeleportSend = args.Send;
        Dirty(teleEnt, teleComp);
        RaiseLocalEvent(teleEnt, args);
        Teleport((teleEnt, teleComp));
    }

    public void OnTeleportBeaconStart(Entity<TeleframeConsoleComponent> ent, ref TeleframeActivateBeaconMessage args)
    {
        if (!TryGetEntity(ent.Comp.LinkedTeleframe, out var teleNetEnt) || !TryComp<TeleframeComponent>(teleNetEnt, out var teleComp))
            return; //if no linked Teleframe, can't teleport.

        //if (!_timing.IsFirstTimePredicted) //prevent it getting spammed
        //    return;

        var teleEnt = teleNetEnt ?? EntityUid.Invalid; //de-nullable teleNetEnt to prevent RaiseLocalEvent getting upset.
        var tp = Transform(GetEntity(args.Beacon.TelePoint)); //get transform of the beacon
        teleComp.Target = _transform.ToMapCoordinates(tp.Coordinates); //coordinates of target, need to be able to replace MapId for beacons
        teleComp.TeleportSend = args.Send;
        Dirty(teleEnt, teleComp);
        RaiseLocalEvent(teleEnt, args);
        Teleport((teleEnt, teleComp));
    }

    public void Teleport(Entity<TeleframeComponent> ent) //could probably move a decent chunk of this to shared but it doesn't really need predicting that much.
    {
        if (HasComp<TeleframeChargingComponent>(ent) || HasComp<TeleframeRechargingComponent>(ent)) //nuh uh, we recharging
            return;

        var ev = new BeforeTeleportEvent(ent);
        RaiseLocalEvent(ent, ev);

        var sourceEffect = ent.Comp.TeleportFromEffect; //default Send teleport, Teleframe From Source to Target
        var targetEffect = ent.Comp.TeleportToEffect;

        //set charge duration before spawning, tryget from protoID?

        if (ent.Comp.TeleportSend != true) //if not the case, reverse. Target to Teleframe.
        {
            sourceEffect = ent.Comp.TeleportToEffect; //alternative Teleframe, Teleframe to Source from Target
            targetEffect = ent.Comp.TeleportFromEffect;
        }

        var tp = Transform(ent); //get transform of the Teleframe
        //Prototype
        Spawn(ent.Comp.TeleportBeginEffect, tp.Coordinates); //flash start effect
        var sourcePortal = Spawn(sourceEffect, tp.Coordinates); //put source portal on Teleframe

        /*if (!TryComp<TwoStageTriggerComponent>(sourcePortal, out var tpStart)) //twostagetrigger breaks all machine AI's if networked so can't dirty it.
        {
            QueueDel(sourcePortal); //no Twostagetrigger, no ride
            Log.Debug("Teleframe Start Portal did not have TwoStageTriggerComponent");
            return;
        }
        tpStart.TriggerDelay = ent.Comp.ChargeDuration; //set duration to component-specific time
        */

        //Log.Debug($"{ent.Comp.Tpx.ToString()},{ent.Comp.Tpx.ToString()}");

        var tpCoords = ent.Comp.Target; //coordinates of target, need to be able to replace MapId for beacons

        Spawn(ent.Comp.TeleportBeginEffect, tpCoords); //flash start effect
        var targetPortal = Spawn(targetEffect, tpCoords); //put target portal on target Coords.
        /*
        if (!TryComp<TwoStageTriggerComponent>(targetPortal, out var tpEnd))
        {
            QueueDel(sourcePortal);
            QueueDel(targetPortal); //no Twostagetrigger, no ride
            Log.Debug("Teleframe End Portal did not have TwoStageTriggerComponent");
            return;
        }
        tpEnd.TriggerDelay = ent.Comp.ChargeDuration; //set duration to component-specific time
        */
        if (ent.Comp.TeleportSend == true)
        {   //send from Source to Target
            /*
            EnsureComp<TeleportOnTriggerComponent>(sourcePortal, out var portalComp);
            portalComp.TeleportFrom = sourcePortal;
            portalComp.TeleportTo = targetPortal;
            portalComp.Teleframe = ent;
            ent.Comp.TeleportFrom = sourcePortal;
            ent.Comp.TeleportTo = targetPortal;
            Dirty(sourcePortal, portalComp);
        }
        else
        {   //receive from Target to Source
            EnsureComp<TeleportOnTriggerComponent>(targetPortal, out var portalComp);
            portalComp.TeleportFrom = targetPortal;
            portalComp.TeleportTo = sourcePortal;
            portalComp.Teleframe = ent;
            ent.Comp.TeleportFrom = targetPortal;
            ent.Comp.TeleportTo = sourcePortal;
            Dirty(targetPortal, portalComp);
            */
        }

        //add power draw here
        //add teleportbegin event here?



        ent.Comp.ReadyToTeleport = false;
        var chargeComp = AddComp<TeleframeChargingComponent>(ent);
        chargeComp.Duration = ent.Comp.ChargeDuration;
        chargeComp.EndTime = ent.Comp.ChargeDuration + Timing.CurTime;
    }

    /// <summary>
    /// If Teleframe and console were linked during map creation, add that link at the start of the round
    /// </summary>
    private void OnMapInit(Entity<TeleframeComponent> ent, ref MapInitEvent args) //stolen from SharedArtifactAnalyzerSystem
    {
        if (!TryComp<DeviceLinkSinkComponent>(ent, out var sink))
            return;

        foreach (var source in sink.LinkedSources)
        {
            if (!TryComp<TeleframeConsoleComponent>(source, out var console))
                continue;

            console.LinkedTeleframe = GetNetEntity(ent);
            ent.Comp.LinkedConsole = source;
            Dirty(source, console);
            Dirty(ent);
            break;
        }
    }

    /// <summary>
    /// links both Teleframe console and Teleframe
    /// </summary>
    private void OnNewLink(Entity<TeleframeConsoleComponent> ent, ref NewLinkEvent args) //stolen from SharedArtifactAnalyzerSystem
    {
        if (TryComp<TeleframeComponent>(args.Sink, out var tp)) //link Teleframe to Teleframe console
        {
            ent.Comp.LinkedTeleframe = GetNetEntity(args.Sink);
            tp.LinkedConsole = ent;
            Dirty(args.Sink, tp);
            Dirty(ent);
        }
    }

    /// <summary>
    /// Disconnects Teleframe Console and Teleframe, setting both sides' Linked variables to null
    /// </summary>
    private void OnPortDisconnected(Entity<TeleframeConsoleComponent> ent, ref PortDisconnectedEvent args) //stolen from SharedArtifactAnalyzerSystem
    {
        var tpNetEntity = ent.Comp.LinkedTeleframe;
        if (args.Port == ent.Comp.LinkingPort && tpNetEntity != null)
        {
            var tpUid = GetEntity(tpNetEntity);
            if (TryComp<TeleframeComponent>(tpUid, out var tp))
            {
                tp.LinkedConsole = null;
                Dirty(tpUid.Value, tp);
            }

            ent.Comp.LinkedTeleframe = null;
            Dirty(ent);
        }
    }

    /// <summary>
    /// Adds the emag flag
    /// </summary>
    private void OnConsoleEmagged(Entity<TeleframeConsoleComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    /// <summary>
    /// Adds the emag flag to the Teleframe, makes the Teleframe more dangerous, cumulative with any other effect that does that.
    /// </summary>
    private void OnTeleframeEmagged(Entity<TeleframeComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        ent.Comp.IncidentChance += 1; //guarenteed chance of incidents
        ent.Comp.IncidentMultiplier += 2; //and they'll be very spicy

        args.Handled = true;
    }

    private void OnTeleport(Entity<TeleframeComponent> ent)
    {
        if (ent.Comp.TeleportFrom == null) //backup for if no TeleframeFrom selecter, choose the Owner.
            ent.Comp.TeleportFrom = ent.Owner;
        if (ent.Comp.TeleportTo == null) //backup for if no Teleframe To, choose Teleport From to just teleport in place
            ent.Comp.TeleportTo = ent.Comp.TeleportFrom;

        var tpFrom = ent.Comp.TeleportFrom ?? ent.Owner; //denullable, shouldn't happen
        var tpTo = ent.Comp.TeleportTo ?? ent.Owner; //denullable, shouldn't happen

        var entities = _lookup.GetEntitiesInRange(tpFrom, ent.Comp.TeleportRadius, flags: LookupFlags.Uncontained); //get everything in teleport radius range that isn't in a container
        int entCount = 0;
        int incidentCount = 0;
        foreach (var tp in entities) //for each entity in list of detected entities
        {
            if (!_physicsQuery.HasComp(tp)) //if it hasn't got physics, skip it, it's probably not meant to be teleported.
                continue;

            var tpEnt = Transform(tp);

            if (tpEnt.Anchored == true) //if it's anchored, skip it. We don't want to be teleporting the Teleframe itself. Or the station's walls.
                continue;

            _transform.DropNextTo(tp, tpTo); //bit scuffed but because the map the target will be on won't neccisarily be the same as the Teleframe we first drop them next to the target THEN scatter.
            var scatterpos = new Vector2(
                _transform.ToMapCoordinates(tpEnt.Coordinates).X + _random.NextFloat(-ent.Comp.TeleportScatterRange, ent.Comp.TeleportScatterRange),
                _transform.ToMapCoordinates(tpEnt.Coordinates).Y + _random.NextFloat(-ent.Comp.TeleportScatterRange, ent.Comp.TeleportScatterRange));

            _transform.SetWorldPosition(tp, scatterpos); //set final position after scatter
            RaiseLocalEvent(tp, new AfterTeleportEvent(tpTo, tpFrom)); //send that teleported entity an event to do something with

            if (_random.NextFloat(0, 1) < ent.Comp.IncidentChance) //roll for teleport incident
            {
                RaiseLocalEvent(tp, new TeleportIncidentEvent(ent.Comp.IncidentMultiplier)); //sent a teleport incident to do something fun with
                incidentCount += 1;
            }
            entCount += 1;
        }
        RaiseLocalEvent(ent.Owner, new AfterTeleportEvent(tpTo, tpFrom)); //denullable it

        var target = Transform(tpTo);
        var from = Transform(tpFrom);
        _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(ent.Owner)} has teleported {entCount} entities from {_transform.ToMapCoordinates(from.Coordinates)} to {_transform.ToMapCoordinates(target.Coordinates)} with {incidentCount} incidents");
    }
}
