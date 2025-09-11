using Content.Shared.Teleportation;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Teleportation.Components;
using Content.Shared.Explosion.Components;
using Content.Shared.Emag.Systems;
using Content.Server.Explosion.Components.OnTrigger;
using Content.Server.Radio.EntitySystems;
using Content.Server.Pinpointer;

namespace Content.Server.Teleportation;

public sealed class TeleporterSystem : SharedTeleporterSystem
{
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleporterComponent, TeleporterActivateMessage>(TeleportCustom);
        SubscribeLocalEvent<TeleporterComponent, TeleporterActivateBeaconMessage>(TeleportBeacon);
        //GotEmaggedEvent
    }
    public override void Update(float frameTime) // need to actually make this work
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

    public void Teleport(Entity<TeleporterComponent> ent) //could probably move a decent chunk of this to shared but it doesn't really need predicting that much.
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
        if (!TryComp<TwoStageTriggerComponent>(sourcePortal, out var tpStart)) //twostagetrigger breaks all machine AI's if networked so can't dirty it.
        {
            QueueDel(sourcePortal); //no Twostagetrigger, no ride
            Log.Debug("Teleporter Start Portal did not have TwoStageTriggerComponent");
            return;
        }
        tpStart.TriggerDelay = ent.Comp.ChargeDuration; //set duration to component-specific time*/

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
        var console = ent.Comp.LinkedConsole ?? EntityUid.Invalid; //denullable
        if (!TryComp<TeleporterConsoleComponent>(console, out var consoleComp)) //console should be linked to get here
            return;

        if (!_emag.CheckFlag(ent.Owner, EmagType.Interaction)) //no speak if emagged
        {
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
                ("proximity", proximity),
                ("map", _maps.TryGetMap(ent.Comp.Target.MapId, out var mapEnt) ? Name(mapEnt ?? EntityUid.Invalid) : Loc.GetString("teleporter-location-unknown"))
            );                                                                  //if mapEnt is null the other option would have been chosen so safe denullable
            _radio.SendRadioMessage(console, message, consoleComp.AnnouncementChannel, console, escapeMarkup: false);
        }
    }

}
