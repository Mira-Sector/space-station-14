//using Content.Shared.Teleportation.Systems;
using Content.Shared.Teleportation.Components;
using Content.Shared.Interaction;
using Content.Shared.Explosion.Components;
using Robust.Shared.Map;

namespace Content.Shared.Teleportation.Systems;

public abstract class SharedTeleporterSystem : EntitySystem
{
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    public override void Initialize()
    {
        base.Initialize();
        Log.Debug("Shared Teleporter Online");

        //SubscribeLocalEvent<TeleporterComponent, ActivateInWorldEvent>(OnInteract);
        //SubscribeLocalEvent<TeleportOnTrigger, TriggerEvent>(OnTeleport);
        //GotEmaggedEvent
    }
    /*
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

        Spawn(ent.Comp.TeleportStartEffect, tp.Coordinates);
        Log.Debug($"{ent.Comp.Tpx.ToString()},{ent.Comp.Tpx.ToString()}");
        var coords = _transform.ToMapCoordinates(tp.Coordinates);
        Log.Debug($"{ent.Comp.Tpx.ToString()},{ent.Comp.Tpx.ToString()}");

        var tpCoords = new MapCoordinates(ent.Comp.Tpx + coords.X, ent.Comp.Tpy + coords.Y, tp.MapID);
        var endPortal = Spawn(endEffect, tpCoords); //put second portal on target GPS Coords or Entity.

        Spawn(ent.Comp.TeleportStartEffect, tpCoords);
        var portalComp = EnsureComp<TeleportOnTriggerComponent>(startPortal);
        if (ent.Comp.TeleportSend == true)
        {
            portalComp = EnsureComp<TeleportOnTriggerComponent>(startPortal);
            portalComp.TeleportFrom = startPortal;
            portalComp.TeleportTo = endPortal;
            ent.Comp.TeleportFrom = startPortal;
            ent.Comp.TeleportTo = endPortal;
        }
        else
        {
            portalComp = EnsureComp<TeleportOnTriggerComponent>(endPortal);
            portalComp.TeleportFrom = endPortal;
            portalComp.TeleportTo = startPortal;
            ent.Comp.TeleportFrom = endPortal;
            ent.Comp.TeleportTo = startPortal;
        }

        var output = portalComp.TeleportFrom ?? ent;
        Log.Debug($"{output.Id}");
        output = portalComp.TeleportTo ?? ent;
        Log.Debug($"{output.Id}");
        //add power draw here
        //add teleportbegin event here?

        //Exists(startPortal);
        //Exists(endPortal);
    }
    */
}
