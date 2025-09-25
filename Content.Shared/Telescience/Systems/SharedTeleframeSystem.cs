using Content.Shared.Telescience.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Telescience.Systems;

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
    [Dependency] protected readonly IRobustRandom Random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleframeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TeleframeComponent, GotEmaggedEvent>(OnTeleframeEmagged);
        SubscribeLocalEvent<TeleframeComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<TeleframeConsoleComponent, TeleframeActivateMessage>(OnTeleportActivate);
        SubscribeLocalEvent<TeleframeConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<TeleframeConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<TeleframeConsoleComponent, GotEmaggedEvent>(OnConsoleEmagged);
    }
    public virtual void OnTeleportSpeak(Entity<TeleframeComponent> ent, string location) { }
    public virtual bool StartTeleport(Entity<TeleframeComponent> ent) { return false; }

    /// <summary>
    /// The initial setup function for teleporting
    /// No need to inform player of fails here as client has the same blockers that do so
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    public void OnTeleportActivate(Entity<TeleframeConsoleComponent> ent, ref TeleframeActivateMessage args)
    {
        if (!Timing.IsFirstTimePredicted) //prevent it getting spammed
            return;

        if (ent.Comp.LinkedTeleframe == null || !TryGetEntity(ent.Comp.LinkedTeleframe, out var teleEnt) || !TryComp<TeleframeComponent>(teleEnt, out var teleComp))
            return; //if null, nonexistent, or lacking teleframe component, return

        if (teleComp.IsPowered == false || teleComp.ReadyToTeleport == false)
            return; //if the teleframe isn't powered or ready, return

        var consoleCoords = Transform(ent).Coordinates;
        if (ent.Comp.MaxRange == null || args.RangeBypass == true || //teleport beacons ignore range limits
        args.Coords.X <= Math.Abs(consoleCoords.X + (float)ent.Comp.MaxRange) && args.Coords.Y <= Math.Abs(consoleCoords.Y + (float)ent.Comp.MaxRange))
        {   //is teleport target coords within MaxRange of current coords
            teleComp.Target = args.Coords; //if successful, update everything
            teleComp.TeleportSend = args.Send;
            Dirty(teleEnt!.Value, teleComp);
            //RaiseLocalEvent(teleEnt!.Value, args); //raise a message on the Teleframe itself, used in preparing a teleport speech
            if (StartTeleport((teleEnt!.Value, teleComp)) == true)
                OnTeleportSpeak((teleEnt!.Value, teleComp), args.Name);
        }
        else
        {
            return; //if requirements not met, return
        }
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

        ent.Comp.IncidentChance += 1;       //guarenteed chance of incidents
        ent.Comp.IncidentMultiplier += 2;   //and they'll be very spicy

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

        _appearance.SetData(ent.Owner, TeleframeVisuals.VisualState, state);
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

            if (HasComp<TeleframeRechargingComponent>(ent))
            {
                args.PushMarkup(Loc.GetString("teleporter-examine-recharging"));
            }
        }
        else
        {
            args.PushMarkup(Loc.GetString("power-receiver-component-on-examine-main", ("stateText", Loc.GetString("power-receiver-component-on-examine-unpowered"))));
        }
    }

    protected (bool, float) RollForIncident(Entity<TeleframeComponent> ent)
    {
        var roll = Random.NextFloat(0, 1);
        if (roll < ent.Comp.IncidentChance)
        {
            return (true, Random.NextFloat(0, 1) * ent.Comp.IncidentMultiplier);
        }
        else
        {
            return (false, 0);
        }
    }

}
