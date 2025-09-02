//using Content.Server.Teleportation.Systems;
using Content.Shared.Teleportation.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
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
        SubscribeLocalEvent<TeleporterComponent, MapInitEvent>(OnMapInit);

        //GotEmaggedEvent
        SubscribeLocalEvent<TeleporterConsoleComponent, TeleporterActivateMessage>(OnTeleportStart);
        SubscribeLocalEvent<TeleporterConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<TeleporterConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
    }

    public void OnTeleportStart(Entity<TeleporterConsoleComponent> ent, ref TeleporterActivateMessage args)
    {
        Log.Debug($"TELEPORT! {args.Coords}");
        if (!TryGetEntity(ent.Comp.LinkedTeleporter, out var teleNetEnt) || !TryComp<TeleporterComponent>(teleNetEnt, out var teleComp))
            return; //if no linked teleporter, can't teleport.

        var teleEnt = teleNetEnt ?? EntityUid.Invalid; //de-nullable teleNetEnt to prevent RaiseLocalEvent getting upset.
        Log.Debug(teleEnt.Id.ToString());

        //(teleComp.Tpx, teleComp.Tpy) = args.Coords;
        //teleComp.TeleportSend = args.Send;
        RaiseLocalEvent(teleEnt, args);
    }

    private void OnMapInit(Entity<TeleporterComponent> ent, ref MapInitEvent args) //stolen from SharedArtifactAnalyzerSystem
    {
        if (!TryComp<DeviceLinkSinkComponent>(ent, out var sink))
            return;

        foreach (var source in sink.LinkedSources)
        {
            if (!TryComp<TeleporterConsoleComponent>(source, out var console))
                continue;

            console.LinkedTeleporter = GetNetEntity(ent);
            ent.Comp.LinkedConsole = source;
            Dirty(source, console);
            Dirty(ent);
            break;
        }
    }

    private void OnNewLink(Entity<TeleporterConsoleComponent> ent, ref NewLinkEvent args) //stolen from SharedArtifactAnalyzerSystem
    {
        if (!TryComp<TeleporterComponent>(args.Sink, out var teleporter))
            return;

        ent.Comp.LinkedTeleporter = GetNetEntity(args.Sink);
        teleporter.LinkedConsole = ent;
        Dirty(args.Sink, teleporter);
        Dirty(ent);
    }

    private void OnPortDisconnected(Entity<TeleporterConsoleComponent> ent, ref PortDisconnectedEvent args) //stolen from SharedArtifactAnalyzerSystem
    {
        var teleporterNetEntity = ent.Comp.LinkedTeleporter;
        if (args.Port != ent.Comp.LinkingPort || teleporterNetEntity == null)
            return;

        var teleporterUid = GetEntity(teleporterNetEntity);
        if (TryComp<TeleporterComponent>(teleporterUid, out var teleporter))
        {
            teleporter.LinkedConsole = null;
            Dirty(teleporterUid.Value, teleporter);
        }

        ent.Comp.LinkedTeleporter = null;
        Dirty(ent);
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
