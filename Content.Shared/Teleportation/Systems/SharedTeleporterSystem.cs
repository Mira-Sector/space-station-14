//using Content.Server.Teleportation.Systems;
using Content.Shared.Teleportation.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.Explosion.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Shared.Teleportation.Systems;

public abstract class SharedTeleporterSystem : EntitySystem
{
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        Log.Debug("Shared Teleporter Online");
        SubscribeLocalEvent<TeleporterComponent, MapInitEvent>(OnMapInit);

        //GotEmaggedEvent
        SubscribeLocalEvent<TeleporterConsoleComponent, ComponentStartup>(OnConsoleStart);
        SubscribeLocalEvent<TeleporterConsoleComponent, TeleporterActivateMessage>(OnTeleportStart);
        SubscribeLocalEvent<TeleporterConsoleComponent, TeleporterActivateBeaconMessage>(OnTeleportBeaconStart);
        SubscribeLocalEvent<TeleporterConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<TeleporterConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);

        SubscribeLocalEvent<TeleporterBeaconComponent, AfterInteractEvent>(OnBeaconInteract);
        SubscribeLocalEvent<TeleporterBeaconComponent, NewLinkEvent>(OnNewBeaconLink);
    }

    public void OnTeleportStart(Entity<TeleporterConsoleComponent> ent, ref TeleporterActivateMessage args)
    {
        if (!TryGetEntity(ent.Comp.LinkedTeleporter, out var teleNetEnt) || !TryComp<TeleporterComponent>(teleNetEnt, out var teleComp))
            return; //if no linked teleporter, can't teleport.

        var teleEnt = teleNetEnt ?? EntityUid.Invalid; //de-nullable teleNetEnt to prevent RaiseLocalEvent getting upset.
        var tp = Transform(teleEnt); //get transform of the teleporter for MapID
        teleComp.Target = new MapCoordinates(args.Coords, tp.MapID); //coordinates of target, need to be able to replace MapId for beacons
        teleComp.TeleportSend = args.Send;
        Dirty(teleEnt, teleComp);
        RaiseLocalEvent(teleEnt, args);
    }

    public void OnTeleportBeaconStart(Entity<TeleporterConsoleComponent> ent, ref TeleporterActivateBeaconMessage args)
    {
        if (!TryGetEntity(ent.Comp.LinkedTeleporter, out var teleNetEnt) || !TryComp<TeleporterComponent>(teleNetEnt, out var teleComp))
            return; //if no linked teleporter, can't teleport.

        var teleEnt = teleNetEnt ?? EntityUid.Invalid; //de-nullable teleNetEnt to prevent RaiseLocalEvent getting upset.
        var tp = Transform(GetEntity(args.Beacon.TelePoint)); //get transform of the beacon
        teleComp.Target = _transform.ToMapCoordinates(tp.Coordinates); //coordinates of target, need to be able to replace MapId for beacons
        teleComp.TeleportSend = args.Send;
        Dirty(teleEnt, teleComp);
        RaiseLocalEvent(teleEnt, args);
    }

    private void OnBeaconInteract(Entity<TeleporterBeaconComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled || !args.CanReach)
            return;

        args.Handled = true;

        if (TryComp<TeleporterConsoleComponent>(args.Target, out var console))
        {
            var newBeacon = new TeleportPoint(Name(ent.Owner), GetNetEntity(ent.Owner));
            if (!console.BeaconList.Contains(newBeacon))
            {
                console.BeaconList.Add(newBeacon);
                Audio.PlayPvs(ent.Comp.LinkSound, ent.Owner);
                _popup.PopupEntity(Loc.GetString("beacon-linked"), ent.Owner, args.User);
            }
            else
            {
                //console.BeaconList.Remove(newBeacon); //if name of beacon has changed, won't clear.
                //Audio.PlayPvs(ent.Comp.LinkSound, ent.Owner);
                //_popup.PopupEntity(Loc.GetString("beacon-unlinked"), ent.Owner, args.User);
            }

            Dirty(args.Target ?? EntityUid.Invalid, console); //denullable to make happy, if args.Target was actually null it shouldn't get here.
            Dirty(ent);
            Log.Debug($"{args.Handled} {args.Target} {ent}");
        }
    }

    private void OnConsoleStart(Entity<TeleporterConsoleComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<TeleporterBeaconComponent>(ent, out var beacon)) //if entity is both a console and a beacon, adds itself to its own beaconlist.
        {
            ent.Comp.BeaconList.Add(new TeleportPoint(Name(ent) + " " + Loc.GetString("teleporter-beacon-self"), GetNetEntity(ent)));
            Dirty(ent, beacon);
        }
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

    private void OnNewBeaconLink(Entity<TeleporterBeaconComponent> ent, ref NewLinkEvent args)
    {
        Log.Debug($"{args.Sink}");
        if (TryComp<TeleporterConsoleComponent>(args.Sink, out var beacon)) //link teleporter beacon to teleporter console
        { //adds both if link has teleportercomponent and teleporterbeaconcomponent?
            beacon.BeaconList.Add(new TeleportPoint(Name(args.Sink), GetNetEntity(args.Sink)));
            Dirty(args.Sink, beacon);
            Dirty(ent);
        }
    }

    private void OnNewLink(Entity<TeleporterConsoleComponent> ent, ref NewLinkEvent args) //stolen from SharedArtifactAnalyzerSystem
    {
        if (TryComp<TeleporterComponent>(args.Sink, out var teleporter)) //link teleporter to teleporter console
        {
            ent.Comp.LinkedTeleporter = GetNetEntity(args.Sink);
            teleporter.LinkedConsole = ent;
            Dirty(args.Sink, teleporter);
            Dirty(ent);
        }
    }

    private void OnPortDisconnected(Entity<TeleporterConsoleComponent> ent, ref PortDisconnectedEvent args) //stolen from SharedArtifactAnalyzerSystem
    {
        var teleporterNetEntity = ent.Comp.LinkedTeleporter;
        if (args.Port == ent.Comp.LinkingPort && teleporterNetEntity != null)
        {
            var teleporterUid = GetEntity(teleporterNetEntity);
            if (TryComp<TeleporterComponent>(teleporterUid, out var teleporter))
            {
                teleporter.LinkedConsole = null;
                Dirty(teleporterUid.Value, teleporter);
            }

            ent.Comp.LinkedTeleporter = null;
            Dirty(ent);
        }
    }
}
