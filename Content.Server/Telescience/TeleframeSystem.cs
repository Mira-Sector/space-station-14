using Content.Shared.Telescience;
using Content.Shared.Telescience.Systems;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Telescience.Components;
using Content.Shared.Explosion.Components;
using Content.Shared.Emag.Systems;
using Content.Server.Explosion.Components.OnTrigger;
using Content.Server.Radio.EntitySystems;
using Content.Server.Pinpointer;
using Content.Server.Chat.Systems;


namespace Content.Server.Telescience;

public sealed class TeleframeSystem : SharedTeleframeSystem
{
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleframeComponent, TeleframeActivateMessage>(TeleportCustom);
        SubscribeLocalEvent<TeleframeComponent, TeleframeActivateBeaconMessage>(TeleportBeacon);

        SubscribeLocalEvent<TeleframeConsoleComponent, TeleframeConsoleSpeak>(OnSpeak);
        //GotEmaggedEvent
    }

    public void TeleportBeacon(Entity<TeleframeComponent> ent, ref TeleframeActivateBeaconMessage args)
    {
        Teleport(ent);
        if (ent.Comp.LinkedConsole != null) //raise event to have console say what the error is
            RaiseLocalEvent(ent.Comp.LinkedConsole ?? EntityUid.Invalid, new TeleframeConsoleSpeak(
                args.Beacon.Location,
                true, true));
        OnTeleportSpeak(ent, args.Beacon.Location);
    }

    public void TeleportCustom(Entity<TeleframeComponent> ent, ref TeleframeActivateMessage args)
    {
        Teleport(ent);
        OnTeleportSpeak(ent, Loc.GetString("Teleframe-target-custom"));
    }

    public void OnTeleportSpeak(Entity<TeleframeComponent> ent, string location) //say over radio that the teleportation is underway.
    {
        if (ent.Comp.LinkedConsole == null) //no point if no console
            return;

        var target = ent.Comp.TeleportSend ? ent.Comp.TeleportTo : ent.Comp.TeleportFrom; //if TeleportSend is true, TeleportTo is target, if false, TeleportFrom is target.
        if (target == null) //null if entityUid's of TeleportTo/From not set, shouldn't happen but we cancel anyway.
            return;
        var targetSafe = target ?? EntityUid.Invalid; //denullable
        string proximity = _navMap.GetNearestBeaconString((targetSafe, Transform(targetSafe)));

        var message = Loc.GetString(
            "Teleframe-console-activate",
            ("send", ent.Comp.TeleportSend),
            ("targetName", location),
            ("X", ent.Comp.Target.Position.X.ToString("0")),
            ("Y", ent.Comp.Target.Position.Y.ToString("0")),
            ("proximity", proximity),
            ("map", _maps.TryGetMap(ent.Comp.Target.MapId, out var mapEnt) ? Name(mapEnt ?? EntityUid.Invalid) : Loc.GetString("Teleframe-location-unknown"))
        );                                                                  //if mapEnt is null the other option would have been chosen so safe denullable

        RaiseLocalEvent(ent.Comp.LinkedConsole ?? EntityUid.Invalid, new TeleframeConsoleSpeak(message, true, true));

    }

    public void OnSpeak(Entity<TeleframeConsoleComponent> ent, ref TeleframeConsoleSpeak args)
    {
        if (!_emag.CheckFlag(ent.Owner, EmagType.Interaction)) //no speak if emagged
        {
            if (args.Voice == true) //speak vocally
                _chat.TrySendInGameICMessage(ent.Owner, args.Message, InGameICChatType.Speak, hideChat: true);
            if (args.Radio == true) //speak over radio
                _radio.SendRadioMessage(ent.Owner, args.Message, ent.Comp.AnnouncementChannel!, ent.Owner, escapeMarkup: false);
        }
    }

}
