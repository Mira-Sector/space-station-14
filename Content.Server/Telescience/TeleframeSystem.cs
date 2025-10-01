using Content.Server.Administration.Logs;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Explosion.Components;
using Content.Shared.Telescience;
using Content.Shared.Telescience.Events;
using Content.Shared.Telescience.Systems;
using Content.Shared.Telescience.Components;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Server.Telescience;

public sealed partial class TeleframeSystem : SharedTeleframeSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    private const LookupFlags RangeFlags = LookupFlags.Approximate | LookupFlags.Dynamic | LookupFlags.Sundries;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleframeComponent, PowerConsumerReceivedChanged>(ReceivedChanged);
        SubscribeLocalEvent<TeleframeComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<TeleframeComponent, ComponentStartup>(OnStartup);

        SubscribeLocalEvent<TeleframeChargingComponent, ComponentStartup>(OnChargeStart);
        SubscribeLocalEvent<TeleframeRechargingComponent, ComponentShutdown>(OnRechargeEnd);
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

            EndTeleportCharge((uid, teleframe, charge));
        }

        //search for Teleframe entities with the TeleframeRechargingComponent and check if they've reached the end of their timer.
        var queryRecharge = EntityQueryEnumerator<TeleframeRechargingComponent, TeleframeComponent>();
        while (queryRecharge.MoveNext(out var uid, out var recharge, out var teleframe))
        {
            if (recharge.Pause || Timing.CurTime < recharge.EndTime)
                continue;

            EndTeleportRecharge((uid, teleframe), recharge);
        }
    }

    /// <summary>
    /// update charge appearance
    /// </summary>
    private void OnChargeStart(Entity<TeleframeChargingComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<TeleframeComponent>(ent, out var teleComp)) //when charging starts, update appearance to charge animation
            return;

        UpdateAppearance((ent.Owner, teleComp));
        if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
            powerConsumer.DrawRate = teleComp.PowerUseActive; // set to high power draw, it actually takes a while to build up due to high demand so this preps for recharge
    }

    private void OnRechargeEnd(Entity<TeleframeRechargingComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<TeleframeComponent>(ent, out var teleComp)) //when recharging ends, update appearance to on animation
            UpdateAppearance((ent.Owner, teleComp));            //recharge component isn't removed if teleframe is depowered
    }

    /// <summary>
    /// When Teleport Charge completes, check whether Teleportation is allowed
    /// </summary>
    public void EndTeleportCharge(Entity<TeleframeComponent, TeleframeChargingComponent> ent)
    {
        if (!Timing.IsFirstTimePredicted) //prevent it getting spammed
            return;

        var failReason = ent.Comp2.FailReason;

        if (ent.Comp1.ActiveTeleportInfo == null)
        {
            ent.Comp2.TeleportSuccess = false; //if either doesn't obvs you can't teleport
            failReason = "teleport-failure-nolink";
        }

        if (ent.Comp2.TeleportSuccess) //if teleport is still good to go, engage
            OnTeleport(ent); //teleport
        else
            TeleportFail(ent, failReason); //if not, say why

        if (HasComp<TeleframeRechargingComponent>(ent))
        {
            RemCompDeferred<TeleframeChargingComponent>(ent); //stop charging
        }
        else
        {
            var rechargeComp = AddComp<TeleframeRechargingComponent>(ent); //start recharging
            rechargeComp.Duration = ent.Comp1.RechargeDuration;
            rechargeComp.EndTime = ent.Comp1.RechargeDuration + Timing.CurTime;
            Dirty(ent, rechargeComp);
        }

        UpdateAppearance(ent);
    }

    ///<summary>
    /// Teleportation has failed, clean up teleportation entities
    /// also summon some l̶i̶g̶h̶t̶n̶i̶n̶g̶ smoke, for fun.
    /// </summary>
    private void TeleportFail(Entity<TeleframeComponent> ent, string failReason)
    {
        if (ent.Comp.ActiveTeleportInfo is { } teleInfo)
        {
            PredictedQueueDel(GetEntity(teleInfo.From));
            PredictedQueueDel(GetEntity(teleInfo.To));
        }

        var pos = Transform(ent).Coordinates;
        SpawnAtPosition("EffectFlashBluespace", pos); //flash
        SpawnAtPosition("WizardSmoke", pos); //and a pop of smoke

        var reasonWrapped = Loc.GetString("teleport-fail", ("reason", failReason));

        var ev = new TelescienceFrameTeleportFailedEvent(reasonWrapped);
        RaiseLocalEvent(ent.Owner, ref ev);
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

        UpdateAppearance(ent);
    }

    /// <summary>
    /// Teleportation Startup
    /// In server because prediction causes it to spam portals regardless of what i do to stop it
    /// </summary>
    /// <param name="ent"></param>
    protected override bool StartTeleport(Entity<TeleframeComponent> ent, TeleframeActivationMode mode, MapCoordinates target)
    {
        if (!Timing.IsFirstTimePredicted) //prevent it getting spammed
            return false;

        if (ent.Comp.ReadyToTeleport != true || HasComp<TeleframeChargingComponent>(ent) || HasComp<TeleframeRechargingComponent>(ent)) //nuh uh, we recharging
            return false;

        var tp = Transform(ent); //get transform of the Teleframe

        var ev = new TelescienceFrameCanTeleportEvent(ent);
        RaiseLocalEvent(ent, ref ev);

        var sourceEffect = ent.Comp.TeleportModeEffects.GetValueOrDefault(mode);
        var targetEffect = ent.Comp.TeleportModeEffects.GetValueOrDefault(mode.GetOpposite());

        Spawn(ent.Comp.TeleportBeginEffect, tp.Coordinates); //flash start effect
        var sourcePortal = Spawn(sourceEffect, tp.Coordinates); //put source portal on Teleframe

        Spawn(ent.Comp.TeleportBeginEffect, target); //flash start effect
        var targetPortal = Spawn(targetEffect, target); //put target portal on target Coords.

        ent.Comp.ActiveTeleportInfo = mode switch
        {
            TeleframeActivationMode.Send => new TeleframeActiveTeleportInfo(mode, GetNetEntity(targetPortal), GetNetEntity(sourcePortal)),
            TeleframeActivationMode.Receive => new TeleframeActiveTeleportInfo(mode, GetNetEntity(sourcePortal), GetNetEntity(targetPortal)),
            _ => throw new NotImplementedException()
        };

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
    /// also adminlog
    /// </summary>
    /// <param name="ent">TeleframeComponent Entity</param>
    private void OnTeleport(Entity<TeleframeComponent> ent)
    {
        if (ent.Comp.ActiveTeleportInfo is not { } teleInfo)
            return;

        var tpFrom = GetEntity(teleInfo.From);
        var tpTo = GetEntity(teleInfo.To);

        var entities = _lookup.GetEntitiesInRange(tpFrom, ent.Comp.TeleportRadius, RangeFlags); //get everything in teleport radius range that isn't in a container
        //getting from inside a container would result in teleporting organs outside of the body, or machine parts outside of machines, this is not good.
        var tpToCoords = _transform.ToMapCoordinates(Transform(tpTo).Coordinates); //have to use map coordinates as these entities will be deleted after teleportation concludes
        var tpFromCoords = _transform.ToMapCoordinates(Transform(tpFrom).Coordinates);

        List<EntityUid> teleported = new(entities.Count);
        foreach (var tp in entities) //for each entity in list of detected entities
        {
            var tpEnt = Transform(tp); //get transform

            if (tpEnt.Anchored) //if it's anchored, skip it. We don't want to be teleporting the Teleframe itself. Or the station's walls.
                continue;

            _transform.DropNextTo(tp, tpTo); //bit scuffed but because the map the target will be on won't neccisarily be the same as the Teleframe we first drop them next to the target THEN scatter.
            var scatterpos = new Vector2( //create scatter coordinates as teleported entities' X and Y values +/- scatter range.
                _transform.ToMapCoordinates(tpEnt.Coordinates).X + Random.NextFloat(-ent.Comp.TeleportScatterRange, ent.Comp.TeleportScatterRange),
                _transform.ToMapCoordinates(tpEnt.Coordinates).Y + Random.NextFloat(-ent.Comp.TeleportScatterRange, ent.Comp.TeleportScatterRange));

            _transform.SetWorldPosition(tp, scatterpos); //set final position after scatter

            var frameEv = new TelescienceFrameTeleportedEvent(tp, tpToCoords, tpFromCoords);
            RaiseLocalEvent(ent.Owner, ref frameEv);

            var tpEv = new TelescienceUserTeleportedEvent(ent.Owner, tpToCoords, tpFromCoords);
            RaiseLocalEvent(tp, ref tpEv);

            teleported.Add(tp);
        }

        var target = Transform(tpTo);
        var from = Transform(tpFrom);
        _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(ent.Owner)} has teleported {teleported.Count} entities from {_transform.ToMapCoordinates(from.Coordinates)} to {_transform.ToMapCoordinates(target.Coordinates)}.");

        var frameFinishEv = new TelescienceFrameTeleportedAllEvent(teleported, tpToCoords, tpFromCoords);
        RaiseLocalEvent(ent.Owner, ref frameFinishEv);

        Spawn(ent.Comp.TeleportFinishEffect, tpToCoords); //finish effects
        Spawn(ent.Comp.TeleportFinishEffect, tpFromCoords);

        // TODO: whenever trigger refactor is merged make this just fire a trigger
        // and use a ComponentRegistry
        EnsureComp<DeleteOnTriggerComponent>(tpTo); //if it doesn't have it for some reason now it does
        RaiseLocalEvent(tpTo, new TriggerEvent(tpTo));

        EnsureComp<DeleteOnTriggerComponent>(tpFrom);
        RaiseLocalEvent(tpFrom, new TriggerEvent(tpTo));

        //clean up
        ent.Comp.ActiveTeleportInfo = null;
        Dirty(ent);
    }

    /// <summary>
    /// checks power situation when spawned
    /// </summary>
    private void OnStartup(Entity<TeleframeComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<PowerConsumerComponent>(ent, out var powerConsume))
            return;

        if (powerConsume.ReceivedPower < powerConsume.DrawRate)
            PowerOff(ent);
        else
            PowerOn(ent);
    }

    /// <summary>
    /// Checks power situation if received amount changes
    /// </summary>
    private void ReceivedChanged(Entity<TeleframeComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        if (Math.Ceiling(args.ReceivedPower) < Math.Floor(args.DrawRate)) //floating point errors at large values
        {
            if (TryComp<TeleframeRechargingComponent>(ent, out var rechargeComp) && args.ReceivedPower > 0) //if recharging and there is some power, don't turn off, just wait.
            {
                rechargeComp.Pause = true;
                rechargeComp.PauseTime = rechargeComp.EndTime - Timing.CurTime;
                Dirty(ent.Owner, rechargeComp);
            }
            else
            {
                if (args.ReceivedPower <= 0)
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
            EndTeleportCharge((ent.Owner, ent.Comp, chargeComp));
            Dirty(ent.Owner, chargeComp);
        }

        if (TryComp<TeleframeRechargingComponent>(ent, out var rechargeComp))
        {
            rechargeComp.Pause = true;
            rechargeComp.PauseTime = rechargeComp.EndTime - Timing.CurTime;
            Dirty(ent.Owner, rechargeComp);
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
        if (HasComp<TeleframeChargingComponent>(ent))
            return;

        if (TryComp<TeleframeRechargingComponent>(ent, out var rechargeComp))
        {
            if (rechargeComp.Pause == true)
            {
                rechargeComp.Pause = false;
                rechargeComp.EndTime = Timing.CurTime + rechargeComp.PauseTime;
                rechargeComp.PauseTime = TimeSpan.FromSeconds(0);
                if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
                    powerConsumer.DrawRate = ent.Comp.PowerUseActive; // set to high power draw as still recharging
                Dirty(ent.Owner, rechargeComp);
            }
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
