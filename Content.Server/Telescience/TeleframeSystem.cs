using Content.Shared.Telescience;
using Content.Shared.Telescience.Systems;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Telescience.Components;
using Content.Shared.Explosion.Components;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Pinpointer;
using Content.Server.Chat.Systems;
using Robust.Shared.Random;
using Robust.Shared.Physics.Components;
using System.Numerics;

namespace Content.Server.Telescience;

public sealed class TeleframeSystem : SharedTeleframeSystem
{
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    private EntityQuery<PhysicsComponent> _physicsQuery; // declare the variable for the query
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleframeComponent, TeleframeActivateMessage>(TeleportCustom);
        SubscribeLocalEvent<TeleframeComponent, TeleframeActivateBeaconMessage>(TeleportBeacon);
        SubscribeLocalEvent<TeleframeComponent, AfterTeleportEvent>(OnTeleportFinish);

        SubscribeLocalEvent<TeleframeConsoleComponent, TeleframeConsoleSpeak>(OnSpeak);
        //GotEmaggedEvent
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;
        //search for Teleframe entities with the TeleframeChargingComponent and check if they've reached the end of their timer.
        var queryCharge = EntityQueryEnumerator<TeleframeChargingComponent, TeleframeComponent>();
        while (queryCharge.MoveNext(out var uid, out var charge, out var teleframe))
        {
            if (Timing.CurTime < charge.EndTime)
                continue;
            EndTeleportCharge((uid, teleframe), (uid, charge));
        }
        //search for Teleframe entities with the TeleframeRechargingComponent and check if they've reached the end of their timer.
        var queryRecharge = EntityQueryEnumerator<TeleframeRechargingComponent, TeleframeComponent>();
        while (queryRecharge.MoveNext(out var uid, out var recharge, out var teleframe))
        {
            if (Timing.CurTime < recharge.EndTime)
                continue;
            EndTeleportRecharge((uid, teleframe), (uid, recharge));
        }

    }

    public void TeleportBeacon(Entity<TeleframeComponent> ent, ref TeleframeActivateBeaconMessage args)
    {
        StartTeleport(ent);
        OnTeleportSpeak(ent, args.Beacon.Location);
    }

    public void TeleportCustom(Entity<TeleframeComponent> ent, ref TeleframeActivateMessage args)
    {
        StartTeleport(ent);
        OnTeleportSpeak(ent, Loc.GetString("teleporter-target-custom"));
    }

    /// <summary>
    /// When Teleport Charge completes, check whether Teleportation is allowed
    /// </summary>
    public void EndTeleportCharge(Entity<TeleframeComponent> ent, Entity<TeleframeChargingComponent> charge)
    {
        if (!Timing.IsFirstTimePredicted) //prevent it getting spammed
            return;

        if (!Exists(ent.Comp.TeleportFrom) || !Exists(ent.Comp.TeleportTo)) //final check that these two exist to teleport from and to
        {
            charge.Comp.TeleportSuccess = false; //if either doesn't obvs you can't teleport
            charge.Comp.FailReason = "nolink";
        }

        if (charge.Comp.TeleportSuccess == true) //if teleport is still good to go, engage
        {
            OnTeleport(ent); //teleport
        }
        else
        {
            TeleportFail(ent, charge.Comp.FailReason); //if not, say why
        }

        if (charge.Comp.WillExplode == true) //and afterwards, if the Teleframe should explode, it does.
            Log.Debug("explode");

        RemCompDeferred<TeleframeChargingComponent>(ent); //stop charging
        var rechargeComp = AddComp<TeleframeRechargingComponent>(ent); //start recharging
        rechargeComp.Duration = ent.Comp.RechargeDuration;
        rechargeComp.EndTime = ent.Comp.RechargeDuration + Timing.CurTime;
        Dirty(ent, rechargeComp);
    }

    ///<summary>
    /// Teleportation has failed, clean up teleportation entities
    /// also summon some l̶i̶g̶h̶t̶n̶i̶n̶g̶ smoke, for fun.
    /// </summary>
    public void TeleportFail(Entity<TeleframeComponent> ent, string failReason)
    {
        EntityManager.PredictedQueueDeleteEntity(ent.Comp.TeleportFrom); //these handle the entity being null so no need for further checks
        EntityManager.PredictedQueueDeleteEntity(ent.Comp.TeleportTo);

        var pos = Transform(ent).Coordinates;
        SpawnAtPosition("EffectFlashBluespace", pos); //flash
        SpawnAtPosition("WizardSmoke", pos); //and a pop of smoke

        if (ent.Comp.LinkedConsole != null) //raise event to have console say what the error is
            RaiseLocalEvent(ent.Comp.LinkedConsole ?? EntityUid.Invalid, new TeleframeConsoleSpeak(
                Loc.GetString("teleport-fail", ("reason", Loc.GetString("teleport-fail-" + failReason))),
                true, true));
    }

    public void EndTeleportRecharge(Entity<TeleframeComponent> ent, Entity<TeleframeRechargingComponent> recharge)
    {
        ent.Comp.ReadyToTeleport = true;
        if (ent.Comp.LinkedConsole != null)
        {
            if (TryComp<TeleframeConsoleComponent>(ent.Comp.LinkedConsole, out var consoleComp))
            {
                Audio.PlayPvs(consoleComp.TeleportRechargedSound, ent.Comp.LinkedConsole ?? EntityUid.Invalid);
            }
        }
        RemCompDeferred<TeleframeRechargingComponent>(ent);
    }

    /// <summary>
    /// Teleportation Startup
    /// In server because prediction causes it to spam portals regardless of what i do to stop it
    /// </summary>
    /// <param name="ent"></param>
    public void StartTeleport(Entity<TeleframeComponent> ent)
    {
        if (!Timing.IsFirstTimePredicted) //prevent it getting spammed
            return;

        Log.Debug("StartTeleport");
        if (ent.Comp.ReadyToTeleport != true || HasComp<TeleframeChargingComponent>(ent) || HasComp<TeleframeRechargingComponent>(ent)) //nuh uh, we recharging
            return;

        if (ent.Comp.TeleportTo != null || ent.Comp.TeleportFrom != null)
            return;

        var ev = new BeforeTeleportEvent(ent);
        RaiseLocalEvent(ent, ev);

        var sourceEffect = ent.Comp.TeleportFromEffect; //default Send teleport, Teleport From Source to Target
        var targetEffect = ent.Comp.TeleportToEffect;

        if (ent.Comp.TeleportSend != true) //if not the case, reverse.
        {
            sourceEffect = ent.Comp.TeleportToEffect; //opposite, Teleport to Source from Target
            targetEffect = ent.Comp.TeleportFromEffect;
        }

        var tp = Transform(ent); //get transform of the Teleframe
        //Prototype
        Spawn(ent.Comp.TeleportBeginEffect, tp.Coordinates); //flash start effect
        var sourcePortal = Spawn(sourceEffect, tp.Coordinates); //put source portal on Teleframe

        //Log.Debug($"{ent.Comp.Tpx.ToString()},{ent.Comp.Tpx.ToString()}");

        var tpCoords = ent.Comp.Target; //coordinates of target

        Spawn(ent.Comp.TeleportBeginEffect, tpCoords); //flash start effect
        var targetPortal = Spawn(targetEffect, tpCoords); //put target portal on target Coords.

        if (ent.Comp.TeleportSend == true)
        {   //send from Source to Target
            ent.Comp.TeleportFrom = sourcePortal;
            ent.Comp.TeleportTo = targetPortal;
        }
        else
        {   //send to Source from Target
            ent.Comp.TeleportTo = sourcePortal;
            ent.Comp.TeleportFrom = targetPortal;
        }

        //add power draw here
        //add teleportbegin event here?
        Log.Debug("TeleframeCharging");
        ent.Comp.ReadyToTeleport = false;
        var chargeComp = AddComp<TeleframeChargingComponent>(ent);
        chargeComp.Duration = ent.Comp.ChargeDuration;
        chargeComp.EndTime = ent.Comp.ChargeDuration + Timing.CurTime;
        Dirty(ent, chargeComp);
    }

    /// <summary>
    /// Function that handles actual teleportation:
    /// Get all entities in range, for each entity
    /// If it doesn't have physics, skip
    /// If it's anchored, skip
    /// otherwise, teleport to target location, then scatter slightly
    /// apply AfterTeleportEvent to them, roll to potentially apply TeleportIncidentEvent as well
    /// also adminlog
    /// </summary>
    /// <param name="ent">TeleframeComponent Entity</param>
    private void OnTeleport(Entity<TeleframeComponent> ent)
    {
        Log.Debug("OnTeleport");
        if (ent.Comp.TeleportFrom == null) //backup for if no TeleportFrom selecter, choose the Owner.
            ent.Comp.TeleportFrom = ent.Owner;
        if (ent.Comp.TeleportTo == null) //backup for if no TeleportTo, choose Teleport From to just teleport in place
            ent.Comp.TeleportTo = ent.Comp.TeleportFrom;

        var tpFrom = ent.Comp.TeleportFrom ?? ent.Owner; //denullable, shouldn't happen
        var tpTo = ent.Comp.TeleportTo ?? ent.Owner; //denullable, shouldn't happen

        var entities = _lookup.GetEntitiesInRange(tpFrom, ent.Comp.TeleportRadius, flags: LookupFlags.Uncontained); //get everything in teleport radius range that isn't in a container
        //getting from inside a container would result in teleporting organs outside of the body, or machine parts outside of machines, this is not good.
        int entCount = 0;
        int incidentCount = 0;
        var tpToCoords = _transform.ToMapCoordinates(Transform(tpTo).Coordinates); //have to use map coordinates as these entities will be deleted after teleportation concludes
        var tpFromCoords = _transform.ToMapCoordinates(Transform(tpFrom).Coordinates);
        foreach (var tp in entities) //for each entity in list of detected entities
        {
            if (!_physicsQuery.HasComp(tp)) //if it hasn't got physics, skip it, it's probably not meant to be teleported.
                continue;

            var tpEnt = Transform(tp); //get transform

            if (tpEnt.Anchored == true) //if it's anchored, skip it. We don't want to be teleporting the Teleframe itself. Or the station's walls.
                continue;

            _transform.DropNextTo(tp, tpTo); //bit scuffed but because the map the target will be on won't neccisarily be the same as the Teleframe we first drop them next to the target THEN scatter.
            var scatterpos = new Vector2( //create scatter coordinates as teleported entities' X and Y values +/- scatter range.
                _transform.ToMapCoordinates(tpEnt.Coordinates).X + _random.NextFloat(-ent.Comp.TeleportScatterRange, ent.Comp.TeleportScatterRange),
                _transform.ToMapCoordinates(tpEnt.Coordinates).Y + _random.NextFloat(-ent.Comp.TeleportScatterRange, ent.Comp.TeleportScatterRange));

            _transform.SetWorldPosition(tp, scatterpos); //set final position after scatter
            RaiseLocalEvent(tp, new AfterTeleportEvent(tpToCoords, tpFromCoords)); //send that teleported entity an event to do something with

            if (_random.NextFloat(0, 1) < ent.Comp.IncidentChance) //roll for teleport incident
            {
                RaiseLocalEvent(tp, new TeleportIncidentEvent(ent.Comp.IncidentMultiplier)); //send a teleport incident to do something fun with
                incidentCount += 1;
            }
            entCount += 1; //iterate number of teleported entities for admin logging purposes
        }
        RaiseLocalEvent(ent.Owner, new AfterTeleportEvent(tpToCoords, tpFromCoords)); //send the teleporter itself an AfterTeleportEvent
        var target = Transform(tpTo);
        var from = Transform(tpFrom);
        _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(ent.Owner)} has teleported {entCount} entities from {_transform.ToMapCoordinates(from.Coordinates)} to {_transform.ToMapCoordinates(target.Coordinates)} with {incidentCount} incidents");
    }

    public void OnTeleportFinish(Entity<TeleframeComponent> ent, ref AfterTeleportEvent args)
    {
        //check upgrades here

        Spawn(ent.Comp.TeleportFinishEffect, args.To); //finish effects
        Spawn(ent.Comp.TeleportFinishEffect, args.From);

        if (ent.Comp.TeleportTo != null) //teleport effects have built in despawn on triggers
            RaiseLocalEvent(ent.Comp.TeleportTo ?? EntityUid.Invalid, new TriggerEvent(ent.Comp.TeleportTo ?? EntityUid.Invalid));
        if (ent.Comp.TeleportFrom != null)
            RaiseLocalEvent(ent.Comp.TeleportFrom ?? EntityUid.Invalid, new TriggerEvent(ent.Comp.TeleportTo ?? EntityUid.Invalid));

        ent.Comp.TeleportTo = null; //clean up
        ent.Comp.TeleportFrom = null;
    }

    public void OnTeleportSpeak(Entity<TeleframeComponent> ent, string location) //say over radio that the teleportation is underway.
    {
        if (ent.Comp.LinkedConsole == null) //no point if no console
            return;

        if (ent.Comp.TeleportTo == null || ent.Comp.TeleportFrom == null) //if teleport entities don't exist, exit.
            return;

        var target = ent.Comp.TeleportSend ? ent.Comp.TeleportTo : ent.Comp.TeleportFrom; //if TeleportSend is true, TeleportTo is target, if false, TeleportFrom is target.
        if (target == null) //null if entityUid's of TeleportTo/From not set, shouldn't happen but we cancel anyway.
            return;
        var targetSafe = target ?? EntityUid.Invalid; //denullable
        string proximity = _navMap.GetNearestBeaconString((targetSafe, Transform(targetSafe)));

        var message = Loc.GetString(
            "teleporter-console-activate",
            ("send", ent.Comp.TeleportSend),
            ("targetName", location),
            ("X", ent.Comp.Target.Position.X.ToString("0")),
            ("Y", ent.Comp.Target.Position.Y.ToString("0")),
            ("proximity", proximity), //contains colour data, which messes with spoken notifications
            ("map", _maps.TryGetMap(ent.Comp.Target.MapId, out var mapEnt) ? Name(mapEnt ?? EntityUid.Invalid) : Loc.GetString("teleporter-location-unknown"))
        );                                                                  //if mapEnt is null the other option would have been chosen so safe denullable

        var linkedConsoleSafe = ent.Comp.LinkedConsole ?? EntityUid.Invalid;
        RaiseLocalEvent(linkedConsoleSafe, new TeleframeConsoleSpeak(message, true, true));

    }

    public void OnSpeak(Entity<TeleframeConsoleComponent> ent, ref TeleframeConsoleSpeak args)
    {
        if (!_emag.CheckFlag(ent.Owner, EmagType.Interaction)) //no speak if emagged
        {
            if (args.Voice == true) //speak vocally
                _chat.TrySendInGameICMessage(ent.Owner, args.Message, InGameICChatType.Speak, hideChat: true);
            if (args.Radio == true && ent.Comp.NoRadio == true) //speak over radio
                _radio.SendRadioMessage(ent.Owner, args.Message, ent.Comp.AnnouncementChannel!, ent.Owner, escapeMarkup: false);
        }
    }


}
