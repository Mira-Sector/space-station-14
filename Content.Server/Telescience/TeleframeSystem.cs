using Content.Shared.Telescience;
using Content.Shared.Telescience.Systems;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Telescience.Components;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Lightning;
using Content.Server.Radio.EntitySystems;
using Content.Server.Pinpointer;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.Components;
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
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    private EntityQuery<PhysicsComponent> _physicsQuery; // declare the variable for the query
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleframeComponent, TeleframeActivateMessage>(OnActivate);
        SubscribeLocalEvent<TeleframeComponent, AfterTeleportEvent>(OnTeleportFinish);
        SubscribeLocalEvent<TeleframeComponent, PowerConsumerReceivedChanged>(ReceivedChanged);
        SubscribeLocalEvent<TeleframeComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<TeleframeComponent, ComponentStartup>(OnStartup);

        SubscribeLocalEvent<TeleframeConsoleComponent, TeleframeConsoleSpeak>(OnSpeak);

        SubscribeLocalEvent<TeleframeChargingComponent, ComponentStartup>(OnChargeStart);
        SubscribeLocalEvent<TeleframeRechargingComponent, ComponentShutdown>(OnRechargeEnd);

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
            EndTeleportCharge((uid, teleframe), charge);
        }
        //search for Teleframe entities with the TeleframeRechargingComponent and check if they've reached the end of their timer.
        var queryRecharge = EntityQueryEnumerator<TeleframeRechargingComponent, TeleframeComponent>();
        while (queryRecharge.MoveNext(out var uid, out var recharge, out var teleframe))
        {
            if (Timing.CurTime < recharge.EndTime || recharge.Pause == true)
                continue;
            EndTeleportRecharge((uid, teleframe), recharge);
        }
    }

    private void OnActivate(Entity<TeleframeComponent> ent, ref TeleframeActivateMessage args)
    {
        OnTeleportSpeak(ent, args.Name);
    }

    /// <summary>
    /// update charge appearance
    /// </summary>
    private void OnChargeStart(Entity<TeleframeChargingComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<TeleframeComponent>(ent, out var teleComp)) //when charging starts, update appearance to charge animation
            UpdateAppearance((ent.Owner, teleComp));
    }
    private void OnRechargeEnd(Entity<TeleframeRechargingComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<TeleframeComponent>(ent, out var teleComp)) //when recharging ends, update apperarance to on animation
            UpdateAppearance((ent.Owner, teleComp));            //recharge component isn't removed if teleframe is depowered
    }

    /// <summary>
    /// When Teleport Charge completes, check whether Teleportation is allowed
    /// </summary>
    public void EndTeleportCharge(Entity<TeleframeComponent> ent, TeleframeChargingComponent charge)
    {
        if (!Timing.IsFirstTimePredicted) //prevent it getting spammed
            return;

        if (!Exists(ent.Comp.TeleportFrom) || !Exists(ent.Comp.TeleportTo)) //final check that these two exist to teleport from and to
        {
            charge.TeleportSuccess = false; //if either doesn't obvs you can't teleport
            charge.FailReason = "nolink";
        }

        if (charge.TeleportSuccess == true) //if teleport is still good to go, engage
        {
            OnTeleport(ent); //teleport
        }
        else
        {
            TeleportFail(ent, charge.FailReason); //if not, say why
        }

        var (roll, score) = RollForIncident(ent); //safe teleportation? Not on my watch
        if (roll == true && score > ent.Comp.ExplosionScore)
            charge.WillExplode = true;

        if (charge.WillExplode == true) //and afterwards, if the Teleframe should explode, it does.
            TeleframeExplode(ent);

        RemCompDeferred<TeleframeChargingComponent>(ent); //stop charging
        if (!HasComp<TeleframeRechargingComponent>(ent))
        {
            var rechargeComp = AddComp<TeleframeRechargingComponent>(ent); //start recharging
            rechargeComp.Duration = ent.Comp.RechargeDuration;
            rechargeComp.EndTime = ent.Comp.RechargeDuration + Timing.CurTime;
            Dirty(ent, rechargeComp);
        }

        if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
            powerConsumer.DrawRate = ent.Comp.PowerUseActive; // set to high power draw to recharge

        Dirty(ent);
        UpdateAppearance(ent);
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
            RaiseLocalEvent(ent.Comp.LinkedConsole!.Value, new TeleframeConsoleSpeak(
                Loc.GetString("teleport-fail", ("reason", Loc.GetString("teleport-fail-" + failReason))),
                true, true));
    }

    ///<summary>
    /// Just fucking explode, lightning bolt number equal to incident multiplier
    ///</summary>
    public void TeleframeExplode(Entity<TeleframeComponent> ent)
    {
        _lightning.ShootRandomLightnings(ent.Owner, ent.Comp.IncidentMultiplier * 3, (int)Math.Ceiling(ent.Comp.IncidentMultiplier));
    }

    /// <summary>
    /// Recharge is done, indicate this to player at console and reset power draw levels
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="recharge"></param>
    public void EndTeleportRecharge(Entity<TeleframeComponent> ent, TeleframeRechargingComponent recharge)
    {
        ent.Comp.ReadyToTeleport = true;
        if (ent.Comp.LinkedConsole != null)
        {
            if (TryComp<TeleframeConsoleComponent>(ent.Comp.LinkedConsole, out var consoleComp))
            {
                Audio.PlayPvs(consoleComp.TeleportRechargedSound, ent.Comp.LinkedConsole!.Value);
            }
        }
        RemCompDeferred<TeleframeRechargingComponent>(ent);
        if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
            powerConsumer.DrawRate = ent.Comp.PowerUseIdle; // recharge end so idle power

        Dirty(ent);
    }

    /// <summary>
    /// Teleportation Startup
    /// In server because prediction causes it to spam portals regardless of what i do to stop it
    /// </summary>
    /// <param name="ent"></param>
    public override bool StartTeleport(Entity<TeleframeComponent> ent)
    {
        if (!Timing.IsFirstTimePredicted) //prevent it getting spammed
            return false;

        if (ent.Comp.ReadyToTeleport != true || HasComp<TeleframeChargingComponent>(ent) || HasComp<TeleframeRechargingComponent>(ent)) //nuh uh, we recharging
            return false;

        var tp = Transform(ent); //get transform of the Teleframe

        var ev = new BeforeTeleportEvent(ent);
        RaiseLocalEvent(ent, ref ev);

        var sourceEffect = ent.Comp.TeleportFromEffect; //default Send teleport, Teleport From Source to Target
        var targetEffect = ent.Comp.TeleportToEffect;

        if (ent.Comp.TeleportSend != true) //if not the case, reverse.
        {
            sourceEffect = ent.Comp.TeleportToEffect; //opposite, Teleport to Source from Target
            targetEffect = ent.Comp.TeleportFromEffect;
        }

        //Prototype
        Spawn(ent.Comp.TeleportBeginEffect, tp.Coordinates); //flash start effect
        var sourcePortal = Spawn(sourceEffect, tp.Coordinates); //put source portal on Teleframe

        //Log.Debug($"{ent.Comp.Tpx.ToString()},{ent.Comp.Tpx.ToString()}");

        var tpCoords = ent.Comp.Target; //coordinates of target

        Log.Debug(tpCoords.Position.ToString());

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
        ent.Comp.ReadyToTeleport = false;
        var chargeComp = AddComp<TeleframeChargingComponent>(ent);
        chargeComp.Duration = ent.Comp.ChargeDuration;
        chargeComp.EndTime = ent.Comp.ChargeDuration + Timing.CurTime;
        Dirty(ent, chargeComp);
        return true;
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
        if (!Exists(ent.Comp.TeleportFrom)) //backup for if no TeleportFrom selecter, choose the Owner.
            ent.Comp.TeleportFrom = ent.Owner;
        if (!Exists(ent.Comp.TeleportTo)) //backup for if no TeleportTo, choose Teleport From to just teleport in place
            ent.Comp.TeleportTo = ent.Comp.TeleportFrom;

        var tpFrom = ent.Comp.TeleportFrom ?? ent.Owner; //denullable, shouldn't happen
        var tpTo = ent.Comp.TeleportTo ?? ent.Owner; //denullable, shouldn't happen

        var entities = _lookup.GetEntitiesInRange(tpFrom, ent.Comp.TeleportRadius, flags: LookupFlags.Uncontained); //get everything in teleport radius range that isn't in a container
        //getting from inside a container would result in teleporting organs outside of the body, or machine parts outside of machines, this is not good.
        int entCount = 0;
        int incidentCount = 0;
        var tpToCoords = _transform.ToMapCoordinates(Transform(tpTo).Coordinates); //have to use map coordinates as these entities will be deleted after teleportation concludes
        var tpFromCoords = _transform.ToMapCoordinates(Transform(tpFrom).Coordinates);
        var afterTeleport = new AfterTeleportEvent(tpToCoords, tpFromCoords);

        foreach (var tp in entities) //for each entity in list of detected entities
        {
            if (!_physicsQuery.HasComp(tp)) //if it hasn't got physics, skip it, it's probably not meant to be teleported.
                continue;

            var tpEnt = Transform(tp); //get transform

            if (tpEnt.Anchored == true) //if it's anchored, skip it. We don't want to be teleporting the Teleframe itself. Or the station's walls.
                continue;

            _transform.DropNextTo(tp, tpTo); //bit scuffed but because the map the target will be on won't neccisarily be the same as the Teleframe we first drop them next to the target THEN scatter.
            var scatterpos = new Vector2( //create scatter coordinates as teleported entities' X and Y values +/- scatter range.
                _transform.ToMapCoordinates(tpEnt.Coordinates).X + Random.NextFloat(-ent.Comp.TeleportScatterRange, ent.Comp.TeleportScatterRange),
                _transform.ToMapCoordinates(tpEnt.Coordinates).Y + Random.NextFloat(-ent.Comp.TeleportScatterRange, ent.Comp.TeleportScatterRange));

            _transform.SetWorldPosition(tp, scatterpos); //set final position after scatter
            RaiseLocalEvent(tp, ref afterTeleport); //send that teleported entity an event to do something with

            var (roll, score) = RollForIncident(ent);
            if (roll == true) //roll for teleport incident
            {
                var teleportIncident = new TeleportIncidentEvent(score, ent.Comp.IncidentMultiplier);
                RaiseLocalEvent(tp, ref teleportIncident); //send a teleport incident to do something fun with
                incidentCount += 1;
            }
            entCount += 1; //iterate number of teleported entities for admin logging purposes
        }
        RaiseLocalEvent(ent.Owner, ref afterTeleport); //send the teleporter itself an AfterTeleportEvent
        var target = Transform(tpTo);
        var from = Transform(tpFrom);
        _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(ent.Owner)} has teleported {entCount} entities from {_transform.ToMapCoordinates(from.Coordinates)} to {_transform.ToMapCoordinates(target.Coordinates)} with {incidentCount} incidents");
    }

    /// <summary>
    /// clean up after teleportation finish
    /// </summary>
    public void OnTeleportFinish(Entity<TeleframeComponent> ent, ref AfterTeleportEvent args)
    {
        //check upgrades here

        Spawn(ent.Comp.TeleportFinishEffect, args.To); //finish effects
        Spawn(ent.Comp.TeleportFinishEffect, args.From);

        if (Exists(ent.Comp.TeleportTo)) //teleport effects have built in despawn on triggers, so call those to end gracefully
        {
            EnsureComp<DeleteOnTriggerComponent>(ent.Comp.TeleportTo!.Value); //if it doesn't have it for some reason now it does
            RaiseLocalEvent(ent.Comp.TeleportTo!.Value, new TriggerEvent(ent.Comp.TeleportTo!.Value));
        }
        if (Exists(ent.Comp.TeleportFrom))
        {
            EnsureComp<DeleteOnTriggerComponent>(ent.Comp.TeleportFrom!.Value);
            RaiseLocalEvent(ent.Comp.TeleportFrom!.Value, new TriggerEvent(ent.Comp.TeleportTo!.Value));
        }

        ent.Comp.TeleportTo = null; //clean up
        ent.Comp.TeleportFrom = null;
    }

    /// <summary>
    /// prepare message to say over radio/voice that the teleportation is underway
    /// </summary>
    public override void OnTeleportSpeak(Entity<TeleframeComponent> ent, string location)
    {
        if (ent.Comp.LinkedConsole == null) //no point if no console
            return;

        if (!Exists(ent.Comp.TeleportTo) || !Exists(ent.Comp.TeleportFrom)) //if teleport entities don't exist, exit.
            return;

        var target = ent.Comp.TeleportSend ? ent.Comp.TeleportTo : ent.Comp.TeleportFrom; //if TeleportSend is true, TeleportTo is target, if false, TeleportFrom is target.
        if (target == null) //null if entityUid's of TeleportTo/From not set, shouldn't happen but we cancel anyway.
            return;
        var targetSafe = target!.Value; //denullable
        string proximity = _navMap.GetNearestBeaconString((targetSafe, Transform(targetSafe)));

        var message = Loc.GetString(
            "teleporter-console-activate",
            ("send", ent.Comp.TeleportSend),
            ("targetName", location),
            ("X", ent.Comp.Target.Position.X.ToString("0")),
            ("Y", ent.Comp.Target.Position.Y.ToString("0")),
            ("proximity", proximity), //contains colour data, which messes with spoken notifications
            ("map", _maps.TryGetMap(ent.Comp.Target.MapId, out var mapEnt) ? Name(mapEnt!.Value) : Loc.GetString("teleporter-location-unknown"))
        );                                                                  //if mapEnt is null the other option would have been chosen so safe denullable

        var linkedConsoleSafe = ent.Comp.LinkedConsole!.Value;
        RaiseLocalEvent(linkedConsoleSafe, new TeleframeConsoleSpeak(message, true, true));

    }

    /// <summary>
    /// checks if speech is allowed, if it is, let console speak
    /// </summary>
    public void OnSpeak(Entity<TeleframeConsoleComponent> ent, ref TeleframeConsoleSpeak args)
    {
        if (!_emag.CheckFlag(ent.Owner, EmagType.Interaction)) //no speak if emagged
        {
            if (args.Voice == true) //speak vocally
                _chat.TrySendInGameICMessage(ent.Owner, args.Message, InGameICChatType.Speak, hideChat: true);
            if (args.Radio == true && ent.Comp.AnnouncementChannel != null) //speak over radio
                _radio.SendRadioMessage(ent.Owner, args.Message, ent.Comp.AnnouncementChannel!.Value, ent.Owner, escapeMarkup: false);
        }
    }

    /// <summary>
    /// checks power situation when spawned
    /// </summary>
    private void OnStartup(Entity<TeleframeComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<PowerConsumerComponent>(ent, out var powerConsume))
        {
            if (powerConsume.ReceivedPower < powerConsume.DrawRate)
            {
                PowerOff(ent);
            }
            else
            {
                PowerOn(ent);
            }
        }
    }

    /// <summary>
    /// Checks power situation if received amount changes
    /// </summary>
    private void ReceivedChanged(Entity<TeleframeComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        if (args.ReceivedPower < args.DrawRate)
        {
            if (TryComp<TeleframeRechargingComponent>(ent, out var rechargeComp) && args.ReceivedPower > 0) //if recharging and there is some power, don't turn off, just wait.
            {
                rechargeComp.Pause = true;
                rechargeComp.PauseTime = rechargeComp.EndTime - Timing.CurTime;
            }
            else
            {
                PowerOff(ent);
            }
        }
        else
        {
            PowerOn(ent);
        }
    }

    /// <summary>
    /// turn off teleframe, interrupt charge and fail it, and pause recharge if it wasn't caught before now
    /// </summary>
    private void PowerOff(Entity<TeleframeComponent> ent)
    {
        ent.Comp.IsPowered = false;
        if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
            powerConsumer.DrawRate = 1; //draw rate is 1 rather than 0 as this means when power is applied a PowerConsumerRecievedChanged event fires to update power again.

        if (TryComp<TeleframeChargingComponent>(ent, out var chargeComp))
        {
            chargeComp.TeleportSuccess = false;
            chargeComp.FailReason = Loc.GetString("teleport-fail-power");
            EndTeleportCharge(ent, chargeComp);
        }

        if (TryComp<TeleframeRechargingComponent>(ent, out var rechargeComp))
        {
            rechargeComp.Pause = true;
            rechargeComp.PauseTime = rechargeComp.EndTime - Timing.CurTime;
        }

        UpdateAppearance(ent);
        Dirty(ent);
    }

    /// <summary>
    /// power on teleframe, unpause recharge if it was there.
    /// </summary>
    private void PowerOn(Entity<TeleframeComponent> ent)
    {
        ent.Comp.IsPowered = true;

        if (TryComp<TeleframeRechargingComponent>(ent, out var rechargeComp))
        {
            rechargeComp.Pause = false;
            rechargeComp.EndTime = Timing.CurTime + rechargeComp.PauseTime;
            rechargeComp.PauseTime = TimeSpan.FromSeconds(0);
            if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
                powerConsumer.DrawRate = ent.Comp.PowerUseActive; // set to high power draw as still recharging
        }
        else
        {
            if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
                powerConsumer.DrawRate = ent.Comp.PowerUseIdle; // set to active power draw
        }

        UpdateAppearance(ent);
        Dirty(ent);
    }

    /// <summary>
    /// immediately turn off if unanchored
    /// </summary>
    private void OnAnchorStateChanged(Entity<TeleframeComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        PowerOff(ent);
    }
}
