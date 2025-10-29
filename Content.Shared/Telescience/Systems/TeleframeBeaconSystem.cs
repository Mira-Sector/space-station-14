using Content.Shared.Telescience.Components;
using Content.Shared.Teleportation.Components;
using Content.Shared.Construction.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Telescience.Systems;

public sealed partial class TeleframeBeaconSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleframeBeaconComponent, AfterInteractEvent>(OnBeaconInteract);
        SubscribeLocalEvent<TeleframeBeaconComponent, NewLinkEvent>(OnNewBeaconLink);
        SubscribeLocalEvent<TeleframeBeaconComponent, AnchorStateChangedEvent>(OnAnchorChange);
        SubscribeLocalEvent<TeleframeBeaconComponent, ComponentStartup>(OnBeaconStart); //consider AnchorEntity and Unanchor Entity using an alt action like Fulton to auto-anchor
    }

    private void OnBeaconInteract(Entity<TeleframeBeaconComponent> ent, ref AfterInteractEvent args) //when beacon is used on a console, add it to the console's beaconlist
    {
        if (!_timing.IsFirstTimePredicted) //prevent it getting spammed
            return;

        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        args.Handled = true;

        if (TryComp<TeleframeConsoleComponent>(args.Target, out var console)) //does target have consolecomponent
        {
            var newBeacon = new TeleportPoint(Name(ent.Owner), GetNetEntity(ent.Owner));
            var present = false;
            foreach (var beacon in console.BeaconList) //can't use .Contains on the hashset as you could change the beacon's name and then it wouldn't be recognised
            {                                          //could do that override thingy but...eeh I don't think this is exactly performance deleting
                if (beacon.TelePoint == newBeacon.TelePoint)
                    present = true;
            } //check all netentities in beaconlist to see if the interacter is already there.

            if (!present) //if not found, add to beaconlist
            {
                console.BeaconList.Add(newBeacon);
                _audio.PlayPredicted(ent.Comp.LinkSound, args.Used, args.User);
                _popup.PopupPredicted(Loc.GetString("beacon-linked"), args.Used, args.User);
            }
            else //if found, remove from beaconlist
            {
                console.BeaconList.Remove(newBeacon);
                _audio.PlayPredicted(ent.Comp.LinkSound, args.Used, args.User);
                _popup.PopupPredicted(Loc.GetString("beacon-unlinked"), args.Used, args.User);
            }

            Dirty(args.Target!.Value, console); //denullable to make happy, if args.Target was actually null it shouldn't get here.
            Dirty(ent);
        }
    }

    private void OnNewBeaconLink(Entity<TeleframeBeaconComponent> ent, ref NewLinkEvent args) //links added via link system, used for static objects
    {
        if (!TryComp<TeleframeConsoleComponent>(args.Sink, out var beacon)) //link Teleframe beacon to Teleframe console
            return;

        if (ent.Owner == args.Sink) //if we're linking to ourselves, indicate such for QoL
            return;

        beacon.BeaconList.Add(new TeleportPoint(Name(ent.Owner), GetNetEntity(ent.Owner)));
        _audio.PlayPvs(ent.Comp.LinkSound, ent.Owner);
        Dirty(ent);
        Dirty(args.Sink, beacon);
    }

    private void OnAnchorChange(Entity<TeleframeBeaconComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!HasComp<AnchorableComponent>(ent)) //if it can be anchored, it needs to be to be valid (although if this is called it probably is)
            return;

        ent.Comp.ValidBeacon = args.Transform.Anchored;
        Dirty(ent);
    }

    private void OnBeaconStart(Entity<TeleframeBeaconComponent> ent, ref ComponentStartup args)
    {
        if (HasComp<AnchorableComponent>(ent)) //if it can be anchored, it needs to be to be valid
        {
            ent.Comp.ValidBeacon = Transform(ent).Anchored;
            Dirty(ent);
        }

        if (TryComp<TeleframeConsoleComponent>(ent, out var consoleComp)) //if it is also a teleframe console, adds itself to its own list
        {
            consoleComp.BeaconList.Add(new TeleportPoint(Loc.GetString("teleporter-beacon-self", ("name", Name(ent.Owner))), GetNetEntity(ent.Owner)));
            Dirty(ent.Owner, consoleComp);
        }
    }


}
