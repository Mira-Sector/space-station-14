using Content.Shared.Teleportation.Systems;
using Content.Shared.Teleportation.Components;
using Content.Shared.Interaction;
using Content.Shared.Explosion.Components;
using Content.Server.Explosion.Components.OnTrigger;
using Robust.Shared.Map;

namespace Content.Server.Teleportation;

public sealed class TeleporterSystem : SharedTeleporterSystem
{
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    public override void Initialize()
    {
        base.Initialize();

        Log.Debug("Server Teleporter Online");

        SubscribeLocalEvent<TeleporterComponent, ActivateInWorldEvent>(OnInteract);
        //SubscribeLocalEvent<TeleportOnTrigger, TriggerEvent>(OnTeleport);
        //GotEmaggedEvent
    }

    private void OnInteract(Entity<TeleporterComponent> ent, ref ActivateInWorldEvent args)
    {
        var startEffect = ent.Comp.TeleportFromEffect; //default Send teleport, Teleporter to Target
        var endEffect = ent.Comp.TeleportToEffect;

        //set charge duration before spawning, tryget from protoID?

        if (ent.Comp.TeleportSend != true) //if not the case, reverse. Target to Teleporter.
        {
            startEffect = ent.Comp.TeleportToEffect;
            endEffect = ent.Comp.TeleportFromEffect;
        }

        var tp = Transform(ent); //get transform of the teleporter
        var startPortal = Spawn(startEffect, tp.Coordinates); //put first portal on Teleporter
        if (!TryComp<TwoStageTriggerComponent>(startPortal, out var tpStart))
        {
            //QueueDel(startPortal); //no Twostagetrigger, no ride
            Log.Debug("Teleporter Start Portal did not have TwoStageTriggerComponent");
            return;
        }
        tpStart.TriggerDelay = ent.Comp.ChargeDuration; //set duration to component-specific time

        Spawn(ent.Comp.TeleportStartEffect, tp.Coordinates);
        Log.Debug($"{ent.Comp.Tpx.ToString()},{ent.Comp.Tpx.ToString()}");
        var coords = _transform.ToMapCoordinates(tp.Coordinates);
        Log.Debug($"{ent.Comp.Tpx.ToString()},{ent.Comp.Tpx.ToString()}");

        var tpCoords = new MapCoordinates(ent.Comp.Tpx + coords.X, ent.Comp.Tpy + coords.Y, tp.MapID);
        var endPortal = Spawn(endEffect, tpCoords); //put second portal on target GPS Coords or Entity.
        if (!TryComp<TwoStageTriggerComponent>(endPortal, out var tpEnd))
        {
            //QueueDel(startPortal);
            //QueueDel(endPortal); //no Twostagetrigger, no ride
            Log.Debug("Teleporter End Portal did not have TwoStageTriggerComponent");
            return;
        }
        tpEnd.TriggerDelay = ent.Comp.ChargeDuration; //set duration to component-specific time
        Spawn(ent.Comp.TeleportStartEffect, tpCoords);
        if (ent.Comp.TeleportSend == true)
        {
            EnsureComp<TeleportOnTriggerComponent>(startPortal, out var portalComp);
            portalComp.TeleportFrom = startPortal;
            portalComp.TeleportTo = endPortal;
            ent.Comp.TeleportFrom = startPortal;
            ent.Comp.TeleportTo = endPortal;
            Dirty(startPortal, portalComp);
        }
        else
        {
            EnsureComp<TeleportOnTriggerComponent>(endPortal, out var portalComp);
            portalComp.TeleportFrom = endPortal;
            portalComp.TeleportTo = startPortal;
            ent.Comp.TeleportFrom = endPortal;
            ent.Comp.TeleportTo = startPortal;
            Dirty(endPortal, portalComp);
        }

        //add power draw here
        //add teleportbegin event here?

        //Exists(startPortal);
        //Exists(endPortal);
    }

}
