using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Events;
using Content.Shared.Telescience.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Telescience;

public sealed partial class TeleframeSystem : SharedTeleframeSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;

    protected override void InitializeRadio()
    {
        base.InitializeRadio();

        SubscribeLocalEvent<TeleframeConsoleRadioComponent, TeleframeToConsoleRelayEvent<TeleframeCanTeleportEvent>>(OnRadioTeleported);
    }

    // in server as nav beacons arent in shared, no idea why as it uses nothing from the server
    /// <summary>
    /// Prepare to send teleport initiation message, gets target and constructs message around its location
    /// </summary>
    private void OnRadioTeleported(Entity<TeleframeConsoleRadioComponent> ent, ref TeleframeToConsoleRelayEvent<TeleframeCanTeleportEvent> args)
    {
        if (args.Frame.Comp.ActiveTeleportInfo is not { } teleInfo)
            return;

        var proximity = _navMap.GetNearestBeaconString(args.Args.Target);

        var message = Loc.GetString(
            "teleporter-console-activate",
            ("send", teleInfo.Mode),
            ("targetName", Identity.Entity(args.Frame.Owner, EntityManager)),
            ("X", args.Args.Target.Position.X.ToString("0")),
            ("Y", args.Args.Target.Position.Y.ToString("0")),
            ("proximity", proximity), //contains colour data, which messes with spoken notifications
            ("map", _maps.TryGetMap(args.Args.Target.MapId, out var mapEnt) ? Name(mapEnt!.Value) : Loc.GetString("teleporter-location-unknown"))
        );                                                                  //if mapEnt is null the other option would have been chosen so safe denullable

        SendRadioMessage(ent, message);
    }
    ///<summary>
    /// Sends radio and/or IC messages as long as the parent device isn't emagged.
    /// </summary>
    protected override void SendRadioMessage(Entity<TeleframeConsoleRadioComponent> ent, string message)
    {
        //no speak if emagged
        if (_emag.CheckFlag(ent.Owner, EmagType.Interaction))
            return;

        if (ent.Comp.SpeakIc)
            _chat.TrySendInGameICMessage(ent.Owner, FormattedMessage.RemoveMarkupOrThrow(message), InGameICChatType.Speak, hideChat: true);

        if (ent.Comp.AnnouncementChannel is { } channel)
            _radio.SendRadioMessage(ent.Owner, message, channel, ent.Owner, escapeMarkup: false);
    }
}
