using Content.Shared.Teleportation.Components;
using Content.Shared.Construction.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared.Teleportation.Systems;

public record struct BeforeTeleportEvent(EntityUid Teleporter, bool Cancelled = false);

public record struct AfterTeleportEvent(EntityUid To, EntityUid From);

public record struct TeleportIncidentEvent(float IncidentMult);

public abstract class SharedTeleporterSystem : EntitySystem
{
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EmagSystem _emag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleporterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TeleporterComponent, GotEmaggedEvent>(OnTeleporterEmagged);

        SubscribeLocalEvent<TeleporterConsoleComponent, TeleporterActivateMessage>(OnTeleportStart);
        SubscribeLocalEvent<TeleporterConsoleComponent, TeleporterActivateBeaconMessage>(OnTeleportBeaconStart);
        SubscribeLocalEvent<TeleporterConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<TeleporterConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<TeleporterConsoleComponent, GotEmaggedEvent>(OnConsoleEmagged);
    }

    public void OnTeleportStart(Entity<TeleporterConsoleComponent> ent, ref TeleporterActivateMessage args)
    {
        if (!TryGetEntity(ent.Comp.LinkedTeleporter, out var teleNetEnt) || !TryComp<TeleporterComponent>(teleNetEnt, out var teleComp))
            return; //if no linked teleporter, can't teleport.

        //if (!_timing.IsFirstTimePredicted) //prevent it getting spammed
        //    return;

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

        //if (!_timing.IsFirstTimePredicted) //prevent it getting spammed
        //    return;

        var teleEnt = teleNetEnt ?? EntityUid.Invalid; //de-nullable teleNetEnt to prevent RaiseLocalEvent getting upset.
        var tp = Transform(GetEntity(args.Beacon.TelePoint)); //get transform of the beacon
        teleComp.Target = _transform.ToMapCoordinates(tp.Coordinates); //coordinates of target, need to be able to replace MapId for beacons
        teleComp.TeleportSend = args.Send;
        Dirty(teleEnt, teleComp);
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
    private void OnConsoleEmagged(Entity<TeleporterConsoleComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    private void OnTeleporterEmagged(Entity<TeleporterComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        ent.Comp.IncidentChance = 1; //makes teleportation spicy
        ent.Comp.IncidentMultiplier = 2;

        args.Handled = true;
    }
}
