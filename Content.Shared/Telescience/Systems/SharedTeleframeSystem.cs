using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Ui;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Map;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTeleframeSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvs = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeIncidents();
        InitializeRelay();
        InitializeRadio();

        SubscribeLocalEvent<TeleframeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TeleframeComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<TeleframeConsoleComponent, TeleframeActivateMessage>(OnTeleportActivate);
        SubscribeLocalEvent<TeleframeConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<TeleframeConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<TeleframeConsoleComponent, GotEmaggedEvent>(OnConsoleEmagged);

        SubscribeLocalEvent<TeleframeConsoleComponent, BoundUIOpenedEvent>(OnUiOpen);
        SubscribeLocalEvent<TeleframeConsoleComponent, BoundUIClosedEvent>(OnUiClosed);
    }

    /// <summary>
    /// The initial setup function for teleporting
    /// No need to inform player of fails here as client has the same blockers that do so
    /// </summary>
    private void OnTeleportActivate(Entity<TeleframeConsoleComponent> ent, ref TeleframeActivateMessage args)
    {
        if (!Timing.IsFirstTimePredicted) //prevent it getting spammed
            return;

        if (ent.Comp.LinkedTeleframe is not { } teleEnt || !TryComp<TeleframeComponent>(teleEnt, out var teleComp))
            return; //if null, nonexistent, or lacking teleframe component, return

        if (!teleComp.IsPowered || !teleComp.ReadyToTeleport)
            return; //if the teleframe isn't powered or ready, return

        var consoleCoords = Transform(ent).Coordinates;

        // this should not be blindly trusting the client
        // TODO: proper validation of input
        if (!args.RangeBypass && ent.Comp.MaxRange is { } maxRange)
        {
            if (args.Coords.MapId != _transform.GetMapId(consoleCoords))
                return;

            var adjustedPos = args.Coords.Offset(consoleCoords.Position);
            if (adjustedPos.Position.LengthSquared() > maxRange * maxRange)
                return;
        }

        if (!StartTeleport((teleEnt, teleComp), args.Mode, args.Coords))
            return;

        Dirty(teleEnt, teleComp);
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

            console.LinkedTeleframe = ent.Owner;
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
            ent.Comp.LinkedTeleframe = args.Sink;
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
        var tpUid = ent.Comp.LinkedTeleframe;
        if (args.Port == ent.Comp.LinkingPort && tpUid != null)
        {
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

    protected virtual bool StartTeleport(Entity<TeleframeComponent> ent, TeleframeActivationMode mode, MapCoordinates target)
    {
        return false;
    }

    /// <summary>
    /// update teleframe appearance between on, off, charged, and recharged
    /// also enables/disables lights
    /// </summary>
    /// <param name="ent"></param>S

    protected void UpdateAppearance(Entity<TeleframeComponent> ent)
    {
        TeleframeVisualState state;
        if (ent.Comp.IsPowered == true) //check if powered, set to on state
        {
            state = TeleframeVisualState.On;
            if (HasComp<TeleframeChargingComponent>(ent)) //override if charged
            {
                state = TeleframeVisualState.Charging;
            }

            if (HasComp<TeleframeRechargingComponent>(ent)) //override if recharged, this state takes highest priority
            {
                state = TeleframeVisualState.Recharging;
            }
        }
        else
        {
            state = TeleframeVisualState.Off;
        }

        if (_lights.TryGetLight(ent.Owner, out var light)) //set light whilst here
        {
            _lights.SetEnabled(ent.Owner, ent.Comp.IsPowered);
            Dirty(ent.Owner, light);
        }

        _appearance.SetData(ent.Owner, TeleframeVisuals.VisualState, state); //Dirties itself
        Dirty(ent);
    }

    /// <summary>
    /// tell user power status and charge level
    /// </summary>
    private void OnExamined(Entity<TeleframeComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.IsPowered == true) //manually apply power level descriptions
        {
            args.PushMarkup(Loc.GetString("power-receiver-component-on-examine-main", ("stateText", Loc.GetString("power-receiver-component-on-examine-powered"))));
            if (HasComp<TeleframeChargingComponent>(ent))
            {
                args.PushMarkup(Loc.GetString("teleporter-examine-charging"));
            }

            if (TryComp<TeleframeRechargingComponent>(ent, out var rechargeComp))
            {
                if (rechargeComp.Pause == false)
                    args.PushMarkup(Loc.GetString("teleporter-examine-recharging"));
                else
                    args.PushMarkup(Loc.GetString("teleporter-examine-recharging-paused"));
            }
        }
        else
        {
            args.PushMarkup(Loc.GetString("power-receiver-component-on-examine-main", ("stateText", Loc.GetString("power-receiver-component-on-examine-unpowered"))));
        }
    }

    /// <summary>
    /// on opening UI add beacons to pvs override list so client can see them outside of view range
    /// </summary>
    private void OnUiOpen(Entity<TeleframeConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(TeleframeConsoleUiKey.Key))
            return;

        if (!_player.TryGetSessionByEntity(args.Actor, out var session)) //one would assume someone interacting with a UI is a player
            return;

        foreach (var beacon in ent.Comp.BeaconList)
        {
            if (TryGetEntity(beacon.TelePoint, out var beaconEnt))
                _pvs.AddSessionOverride(beaconEnt.Value, session);
            else
                ent.Comp.BeaconList.Remove(beacon); //do some housecleaning and remove beacons that have been deleted outright.

            if (ent.Comp.LinkedTeleframe != null)
                _pvs.AddSessionOverride(ent.Comp.LinkedTeleframe.Value, session);
        }

        Dirty(ent);
    }

    /// <summary>
    /// on closing UI remove beacons from pvs list again
    /// </summary>
    private void OnUiClosed(Entity<TeleframeConsoleComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(TeleframeConsoleUiKey.Key))
            return;

        if (!_player.TryGetSessionByEntity(args.Actor, out var session)) //one would assume someone interacting with a UI is a player
            return;

        foreach (var beacon in ent.Comp.BeaconList)
        {
            if (TryGetEntity(beacon.TelePoint, out var beaconEnt))
                _pvs.RemoveSessionOverride(beaconEnt.Value, session);
            else
                ent.Comp.BeaconList.Remove(beacon); //do some housecleaning and remove beacons that have been deleted outright.
        }

        if (ent.Comp.LinkedTeleframe != null)
            _pvs.RemoveSessionOverride(ent.Comp.LinkedTeleframe.Value, session);

        Dirty(ent);
    }
}
