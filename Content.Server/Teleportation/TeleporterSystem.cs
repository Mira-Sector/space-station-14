using Content.Shared.Teleportation;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Teleportation.Components;
using Content.Shared.Interaction;
using Content.Shared.Explosion.Components;
using Content.Shared.Emag.Systems;
using Content.Server.Explosion.Components.OnTrigger;
using Content.Server.Radio.EntitySystems;
using Content.Server.Pinpointer;
using Robust.Shared.Map;

namespace Content.Server.Teleportation;

public sealed class TeleporterSystem : SharedTeleporterSystem
{
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleporterComponent, TeleporterActivateMessage>(TeleportCustom);
        SubscribeLocalEvent<TeleporterComponent, TeleporterActivateBeaconMessage>(TeleportBeacon);
        //GotEmaggedEvent
    }
    
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TeleporterComponent>();
        while (query.MoveNext(out var uid, out var teleporter))
        {
            if (teleporter.ReadyToTeleport.TotalSeconds <= 0)
                continue;

            teleporter.ReadyToTeleport -= TimeSpan.FromSeconds(frameTime);
        }
    }

    public void TeleportBeacon(Entity<TeleporterComponent> ent, ref TeleporterActivateBeaconMessage args)
    {
        Teleport(ent);
        OnTeleportSpeak(ent, args.Beacon.Location);
    }

    public void TeleportCustom(Entity<TeleporterComponent> ent, ref TeleporterActivateMessage args)
    {
        Teleport(ent);
        OnTeleportSpeak(ent, Loc.GetString("teleport-target-custom"));
    }

    public void Teleport(Entity<TeleporterComponent> ent)
    {
        if (ent.Comp.ReadyToTeleport.TotalSeconds > 0) //nuh uh, we recharging
            return;

        ent.Comp.ReadyToTeleport = ent.Comp.ChargeDuration;
        var ev = new BeforeTeleportEvent(ent);
        RaiseLocalEvent(ent, ev);

        var sourceEffect = ent.Comp.TeleportFromEffect; //default Send teleport, Teleporter From Source to Target
        var targetEffect = ent.Comp.TeleportToEffect;

        //set charge duration before spawning, tryget from protoID?

        if (ent.Comp.TeleportSend != true) //if not the case, reverse. Target to Teleporter.
        {
            sourceEffect = ent.Comp.TeleportToEffect; //alternative teleporter, Teleporter to Source from Target
            targetEffect = ent.Comp.TeleportFromEffect;
        }

        var tp = Transform(ent); //get transform of the teleporter
        //Prototype
        Spawn(ent.Comp.TeleportBeginEffect, tp.Coordinates); //flash start effect
        var sourcePortal = Spawn(sourceEffect, tp.Coordinates); //put source portal on Teleporter
        if (!TryComp<TwoStageTriggerComponent>(sourcePortal, out var tpStart))
        {
            QueueDel(sourcePortal); //no Twostagetrigger, no ride
            Log.Debug("Teleporter Start Portal did not have TwoStageTriggerComponent");
            return;
        }
        tpStart.TriggerDelay = ent.Comp.ChargeDuration; //set duration to component-specific time*/
        Dirty(sourcePortal, tpStart);

        //Log.Debug($"{ent.Comp.Tpx.ToString()},{ent.Comp.Tpx.ToString()}");

        var tpCoords = ent.Comp.Target; //coordinates of target, need to be able to replace MapId for beacons

        Spawn(ent.Comp.TeleportBeginEffect, tpCoords); //flash start effect
        var targetPortal = Spawn(targetEffect, tpCoords); //put target portal on target Coords.
        if (!TryComp<TwoStageTriggerComponent>(targetPortal, out var tpEnd))
        {
            QueueDel(sourcePortal);
            QueueDel(targetPortal); //no Twostagetrigger, no ride
            Log.Debug("Teleporter End Portal did not have TwoStageTriggerComponent");
            return;
        }
        tpEnd.TriggerDelay = ent.Comp.ChargeDuration; //set duration to component-specific time*/
        Dirty(targetPortal, tpEnd);

        if (ent.Comp.TeleportSend == true)
        {   //send from Source to Target
            EnsureComp<TeleportOnTriggerComponent>(sourcePortal, out var portalComp);
            portalComp.TeleportFrom = sourcePortal;
            portalComp.TeleportTo = targetPortal;
            portalComp.Teleporter = ent;
            ent.Comp.TeleportFrom = sourcePortal;
            ent.Comp.TeleportTo = targetPortal;
            Dirty(sourcePortal, portalComp);
        }
        else
        {   //receive from Target to Source
            EnsureComp<TeleportOnTriggerComponent>(targetPortal, out var portalComp);
            portalComp.TeleportFrom = targetPortal;
            portalComp.TeleportTo = sourcePortal;
            portalComp.Teleporter = ent;
            ent.Comp.TeleportFrom = targetPortal;
            ent.Comp.TeleportTo = sourcePortal;
            Dirty(targetPortal, portalComp);
        }

        //add power draw here
        //add teleportbegin event here?

        //Exists(sourcePortal);
        //Exists(targetPortal);
    }

    public void OnTeleportSpeak(Entity<TeleporterComponent> ent, string location) //say over radio that the teleportation is underway.
    {
        //Loc.GetString("teleporter-initiate", send
        var console = ent.Comp.LinkedConsole ?? EntityUid.Invalid; //denullable
        if (!TryComp<TeleporterConsoleComponent>(console, out var consoleComp))
            return;

        if (!_emag.CheckFlag(ent.Owner, EmagType.Interaction))
        {
            var target = ent.Comp.TeleportSend ? ent.Comp.TeleportTo : ent.Comp.TeleportFrom;
            if (target == null)
                return;
            var targetSafe = target ?? EntityUid.Invalid; //denullable
            string proximity = _navMap.GetNearestBeaconString((targetSafe, Transform(targetSafe)));

            var message = Loc.GetString(
                "teleport-console-activate",
                ("send", ent.Comp.TeleportSend),
                ("targetName", location),
                ("target", ent.Comp.Target.Position),
                ("proximity", proximity)
            );
            _radio.SendRadioMessage(console, message, consoleComp.AnnouncementChannel, console, escapeMarkup: false);
        }
    }

}
