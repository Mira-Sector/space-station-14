using Content.Shared.Telescience.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Construction.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.Spawners;
using Robust.Shared.Physics.Components;


namespace Content.Shared.Telescience.Systems;

public record struct BeforeTeleportEvent(EntityUid Teleframe, bool Cancelled = false);

public record struct AfterTeleportEvent(MapCoordinates To, MapCoordinates From);

public record struct TeleportIncidentEvent(float IncidentMult);

public record struct TeleframeConsoleSpeak(string Message, bool Radio, bool Voice);

public abstract class SharedTeleframeSystem : EntitySystem
{

    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleframeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TeleframeComponent, GotEmaggedEvent>(OnTeleframeEmagged);


        SubscribeLocalEvent<TeleframeConsoleComponent, TeleframeActivateMessage>(OnTeleportStart);
        SubscribeLocalEvent<TeleframeConsoleComponent, TeleframeActivateBeaconMessage>(OnTeleportBeaconStart);
        SubscribeLocalEvent<TeleframeConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<TeleframeConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<TeleframeConsoleComponent, GotEmaggedEvent>(OnConsoleEmagged);
    }
    public void OnTeleportStart(Entity<TeleframeConsoleComponent> ent, ref TeleframeActivateMessage args)
    {
        if (!Timing.IsFirstTimePredicted) //prevent it getting spammed
            return;

        if (!TryGetEntity(ent.Comp.LinkedTeleframe, out var teleNetEnt) || !TryComp<TeleframeComponent>(teleNetEnt, out var teleComp))
            return; //if no linked Teleframe, can't teleport.

        if (teleComp.IsPowered == false || teleComp.ReadyToTeleport == false)
            return;

        var teleEnt = teleNetEnt ?? EntityUid.Invalid; //de-nullable teleNetEnt to prevent RaiseLocalEvent getting upset.
        var tp = Transform(teleEnt); //get transform of the Teleframe for MapID
        teleComp.Target = new MapCoordinates(args.Coords, tp.MapID); //coordinates of target, need to be able to replace MapId for beacons
        teleComp.TeleportSend = args.Send;
        Dirty(teleEnt, teleComp);
        RaiseLocalEvent(teleEnt, args); //raise a message on the Teleframe itself, used in generating teleport speech
    }

    public void OnTeleportBeaconStart(Entity<TeleframeConsoleComponent> ent, ref TeleframeActivateBeaconMessage args)
    {
        if (!Timing.IsFirstTimePredicted) //prevent it getting spammed
            return;

        if (!TryGetEntity(ent.Comp.LinkedTeleframe, out var teleNetEnt) || !TryComp<TeleframeComponent>(teleNetEnt, out var teleComp))
            return; //if no linked Teleframe, can't teleport.

        if (teleComp.IsPowered == false || teleComp.ReadyToTeleport == false)
            return;

        var teleEnt = teleNetEnt ?? EntityUid.Invalid; //de-nullable teleNetEnt to prevent RaiseLocalEvent getting upset.
        var tp = Transform(GetEntity(args.Beacon.TelePoint)); //get transform of the beacon
        teleComp.Target = _transform.ToMapCoordinates(tp.Coordinates); //coordinates of target, need to be able to replace MapId for beacons
        teleComp.TeleportSend = args.Send;
        Dirty(teleEnt, teleComp);
        RaiseLocalEvent(teleEnt, args); //raise a message on the Teleframe itself, used in generating teleport speech
    }

    /// <summary>
    /// If Teleframe and console were linked during map creation, add that link at the start of the round
    /// </summary>
    private void OnMapInit(Entity<TeleframeComponent> ent, ref MapInitEvent args) //stolen from SharedArtifactAnalyzerSystem
    {
        if (!TryComp<DeviceLinkSinkComponent>(ent, out var sink))
            return;

        foreach (var source in sink.LinkedSources)
        {
            if (!TryComp<TeleframeConsoleComponent>(source, out var console))
                continue;

            console.LinkedTeleframe = GetNetEntity(ent);
            ent.Comp.LinkedConsole = source;
            Dirty(source, console);
            Dirty(ent);
            break;
        }
    }

    /// <summary>
    /// links both Teleframe console and Teleframe
    /// </summary>
    private void OnNewLink(Entity<TeleframeConsoleComponent> ent, ref NewLinkEvent args) //stolen from SharedArtifactAnalyzerSystem
    {
        if (TryComp<TeleframeComponent>(args.Sink, out var tp)) //link Teleframe to Teleframe console
        {
            ent.Comp.LinkedTeleframe = GetNetEntity(args.Sink);
            tp.LinkedConsole = ent;
            Dirty(args.Sink, tp);
            Dirty(ent);
        }
    }

    /// <summary>
    /// Disconnects Teleframe Console and Teleframe, setting both sides' Linked variables to null
    /// </summary>
    private void OnPortDisconnected(Entity<TeleframeConsoleComponent> ent, ref PortDisconnectedEvent args) //stolen from SharedArtifactAnalyzerSystem
    {
        var tpNetEntity = ent.Comp.LinkedTeleframe;
        if (args.Port == ent.Comp.LinkingPort && tpNetEntity != null)
        {
            var tpUid = GetEntity(tpNetEntity);
            if (TryComp<TeleframeComponent>(tpUid, out var tp))
            {
                tp.LinkedConsole = null;
                Dirty(tpUid.Value, tp);
            }

            ent.Comp.LinkedTeleframe = null;
            Dirty(ent);
        }
    }

    /// <summary>
    /// Adds the emag flag
    /// </summary>
    private void OnConsoleEmagged(Entity<TeleframeConsoleComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    /// <summary>
    /// Adds the emag flag to the Teleframe, makes the Teleframe more dangerous, cumulative with any other effect that does that.
    /// </summary>
    private void OnTeleframeEmagged(Entity<TeleframeComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        ent.Comp.IncidentChance += 1; //guarenteed chance of incidents
        ent.Comp.IncidentMultiplier += 2; //and they'll be very spicy

        args.Handled = true;
    }

    /// <summary>
    /// update teleframe appearence between on, off, charged, and recharged
    /// also enables/disables lights
    /// </summary>
    /// <param name="ent"></param>S

    protected void UpdateAppearance(Entity<TeleframeComponent> ent)
    {
        TeleframeVisualState state;
        if (ent.Comp.IsPowered == true)
        {
            state = TeleframeVisualState.On;
            if (HasComp<TeleframeChargingComponent>(ent))
            {
                state = TeleframeVisualState.Charging;
            }

            if (HasComp<TeleframeRechargingComponent>(ent))
            {
                state = TeleframeVisualState.Recharging;
            }
        }
        else
        {
            state = TeleframeVisualState.Off;
        }

        if (_lights.TryGetLight(ent.Owner, out var light))
            _lights.SetEnabled(ent.Owner, ent.Comp.IsPowered);

        _appearance.SetData(ent.Owner, TeleframeVisuals.VisualState, state);
        Dirty(ent);
    }

}
