using Content.Shared.Teleportation.Components;
using Content.Shared.Construction.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Teleportation.Systems;

public abstract class SharedTeleporterBeaconSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TeleporterConsoleComponent, ComponentStartup>(OnConsoleStart);

        SubscribeLocalEvent<TeleporterBeaconComponent, AfterInteractEvent>(OnBeaconInteract);
        SubscribeLocalEvent<TeleporterBeaconComponent, NewLinkEvent>(OnNewBeaconLink);
        SubscribeLocalEvent<TeleporterBeaconComponent, AnchorStateChangedEvent>(OnAnchorChange);
        SubscribeLocalEvent<TeleporterBeaconComponent, ComponentStartup>(OnBeaconStart); //consider AnchorEntity and Unanchor Entity using an alt action like Fultonn to auto-anchor
    }

    private void OnConsoleStart(Entity<TeleporterConsoleComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<TeleporterBeaconComponent>(ent, out var beacon)) //if entity is both a console and a beacon, adds itself to its own beaconlist.
        {
            ent.Comp.BeaconList.Add(new TeleportPoint(Name(ent) + " " + Loc.GetString("teleporter-beacon-self"), GetNetEntity(ent)));
            Dirty(ent, beacon);
        }
    }

    private void OnBeaconInteract(Entity<TeleporterBeaconComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled || !args.CanReach)
            return;

        args.Handled = true;

        if (!_timing.IsFirstTimePredicted) //prevent it getting spammed
            return;

        if (TryComp<TeleporterConsoleComponent>(args.Target, out var console))
        {
            var newBeacon = new TeleportPoint(Name(ent.Owner), GetNetEntity(ent.Owner));
            var present = false;
            foreach (var beacon in console.BeaconList) //can't use .Contains on the hashset as you could change the beacon's name and then it wouldn't be recognised
            {
                if (beacon.TelePoint == newBeacon.TelePoint)
                    present = true;
            }

            if (present == false)
            {
                console.BeaconList.Add(newBeacon);
                Audio.PlayPvs(ent.Comp.LinkSound, ent.Owner);
                _popup.PopupEntity(Loc.GetString("beacon-linked"), ent.Owner, args.User);
            }
            else
            {
                console.BeaconList.Remove(newBeacon);
                Audio.PlayPvs(ent.Comp.LinkSound, ent.Owner);
                _popup.PopupEntity(Loc.GetString("beacon-unlinked"), ent.Owner, args.User);
            }
            Log.Debug($"{args.Handled} {args.Target} {ent}");
            Dirty(args.Target ?? EntityUid.Invalid, console); //denullable to make happy, if args.Target was actually null it shouldn't get here.
            Dirty(ent);
        }
    }
    private void OnNewBeaconLink(Entity<TeleporterBeaconComponent> ent, ref NewLinkEvent args) //links added via link system, used for static objects
    {
        Log.Debug($"{args.Sink} {args.Source}");
        if (TryComp<TeleporterConsoleComponent>(args.Sink, out var beacon)) //link teleporter beacon to teleporter console
        { //adds both if link has teleportercomponent and teleporterbeaconcomponent?
            beacon.BeaconList.Add(new TeleportPoint(Name(ent.Owner), GetNetEntity(ent.Owner)));
            Audio.PlayPvs(ent.Comp.LinkSound, ent.Owner);
            Dirty(args.Sink, beacon);
            Dirty(ent);
        }
    }

    private void OnAnchorChange(Entity<TeleporterBeaconComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (TryComp<AnchorableComponent>(ent, out var _)) //if it can be anchored, it needs to be to be valid (although if this is called on it it probably is)
            ent.Comp.ValidBeacon = args.Transform.Anchored;
        Dirty(ent);
    }

    private void OnBeaconStart(Entity<TeleporterBeaconComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<AnchorableComponent>(ent, out var _)) //if it can be anchored, it needs to be to be valid
            ent.Comp.ValidBeacon = Transform(ent).Anchored;
        Dirty(ent);
    }

}