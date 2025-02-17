using Content.Shared.Access.Components;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vehicles;

public abstract partial class SharedVehicleSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public static readonly EntProtoId HornActionId = "ActionHorn";
    public static readonly EntProtoId SirenActionId = "ActionSiren";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VehicleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VehicleComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<VehicleComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<VehicleComponent, StrappedEvent>(OnStrapped);

        SubscribeLocalEvent<VehicleComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<VehicleComponent, VirtualItemDeletedEvent>(OnDropped);

        SubscribeLocalEvent<VehicleComponent, EntInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<VehicleComponent, EntRemovedFromContainerMessage>(OnEject);

        SubscribeLocalEvent<VehicleComponent, HornActionEvent>(OnHorn);
        SubscribeLocalEvent<VehicleComponent, SirenActionEvent>(OnSiren);

        SubscribeLocalEvent<VehicleComponent, GetAdditionalAccessEvent>(OnGetAdditionalAccess);
    }

    private void OnInit(EntityUid uid, VehicleComponent component, ComponentInit args)
    {
        _appearance.SetData(uid, VehicleState.Animated, component.EngineRunning);
        _appearance.SetData(uid, VehicleState.DrawOver, false);
    }

    private void OnRemove(EntityUid uid, VehicleComponent component, ComponentRemove args)
    {
        if (component.Driver == null)
            return;

        _buckle.TryUnbuckle(component.Driver.Value, component.Driver.Value);
        Dismount(component.Driver.Value, uid);
        _appearance.SetData(uid, VehicleState.DrawOver, false);
    }

    private void OnInsert(EntityUid uid, VehicleComponent component, ref EntInsertedIntoContainerMessage args)
    {
        if (!ItemSlotWhitelist(uid, args.Entity))
            return;

        component.EngineRunning = true;
        _appearance.SetData(uid, VehicleState.Animated, true);

        _ambientSound.SetAmbience(uid, true);

        if (component.Driver == null)
            return;

        Mount(component.Driver.Value, uid);
    }

    private void OnEject(EntityUid uid, VehicleComponent component, ref EntRemovedFromContainerMessage args)
    {
        if (!ItemSlotWhitelist(uid, args.Entity))
            return;

        component.EngineRunning = false;
        _appearance.SetData(uid, VehicleState.Animated, false);

        _ambientSound.SetAmbience(uid, false);

        if (component.Driver == null)
            return;

        Dismount(component.Driver.Value, uid);
    }

    private bool ItemSlotWhitelist(EntityUid vehicle, EntityUid item)
    {
        if (!TryComp<ItemSlotsComponent>(vehicle, out var slotsComp))
            return false;

        var passed = false;

        foreach (var slot in slotsComp.Slots.Values)
        {
            if (!_whitelist.CheckBoth(item, slot.Blacklist, slot.Whitelist))
                continue;

            passed = true;
            break;
        }

        return passed;
    }

    private void OnHorn(EntityUid uid, VehicleComponent component, InstantActionEvent args)
    {
        if (args.Handled == true)
            return;

        if (component.Driver != args.Performer)
            return;

        if (component.HornSound == null)
            return;

        _audio.PlayPvs(component.HornSound, uid);
        args.Handled = true;
    }

    private void OnSiren(EntityUid uid, VehicleComponent component, InstantActionEvent args)
    {
        if (args.Handled == true)
            return;

        if (component.Driver != args.Performer)
            return;

        if (component.SirenSound == null)
            return;

        if (component.SirenEnabled)
        {
            component.SirenStream = _audio.Stop(component.SirenStream);
        }
        else
        {
            var audio = _audio.PlayPvs(component.SirenSound, uid, AudioParams.Default.WithLoop(true).WithMaxDistance(5));

            if (audio != null)
                component.SirenStream = audio.Value.Entity;
        }

        component.SirenEnabled = !component.SirenEnabled;
        args.Handled = true;
    }


    private void OnStrapAttempt(Entity<VehicleComponent> ent, ref StrapAttemptEvent args)
    {
        var driver = args.Buckle.Owner; // i dont want to re write this shit 100 fucking times

        if (ent.Comp.Driver != null)
        {
            args.Cancelled = true;
            return;
        }

        if (ent.Comp.RequiredHands != 0)
        {
            for (int hands = 0; hands < ent.Comp.RequiredHands; hands++)
            {
                if (!_virtualItem.TrySpawnVirtualItemInHand(ent.Owner, driver, false))
                {
                    args.Cancelled = true;
                    _virtualItem.DeleteInHandsMatching(driver, ent.Owner);
                    return;
                }
            }
        }

        AddHorns(driver, ent);
    }

    private void OnStrapped(Entity<VehicleComponent> ent, ref StrappedEvent args)
    {
        var driver = args.Buckle.Owner;

        if (!TryComp(driver, out MobMoverComponent? mover))
            return;

        if (ent.Comp.Driver != null)
            return;

        ent.Comp.Driver = driver;
        _appearance.SetData(ent.Owner, VehicleState.DrawOver, true);

        if (!ent.Comp.EngineRunning)
            return;

        Mount(driver, ent.Owner);
    }

    private void OnUnstrapped(Entity<VehicleComponent> ent, ref UnstrappedEvent args)
    {
        if (ent.Comp.Driver != args.Buckle.Owner)
            return;

        Dismount(args.Buckle.Owner, ent);
        _appearance.SetData(ent.Owner, VehicleState.DrawOver, false);
    }

    private void OnDropped(EntityUid uid, VehicleComponent comp, VirtualItemDeletedEvent args)
    {
        if (comp.Driver != args.User)
            return;

        _buckle.TryUnbuckle(args.User, args.User);

        Dismount(args.User, uid);
        _appearance.SetData(uid, VehicleState.DrawOver, false);
    }

    private void AddHorns(EntityUid driver, EntityUid vehicle)
    {
        if (!TryComp<VehicleComponent>(vehicle, out var vehicleComp))
            return;

        if (vehicleComp.HornSound != null)
            _actions.AddAction(driver, ref vehicleComp.HornAction, HornActionId, vehicle);

        if (vehicleComp.SirenSound != null)
            _actions.AddAction(driver, ref vehicleComp.SirenAction, SirenActionId, vehicle);
    }

    private void Mount(EntityUid driver, EntityUid vehicle)
    {
        _mover.SetRelay(driver, vehicle);
    }

    private void Dismount(EntityUid driver, EntityUid vehicle)
    {
        if (!TryComp<VehicleComponent>(vehicle, out var vehicleComp))
            return;

        if (vehicleComp.Driver != driver)
            return;

        RemComp<RelayInputMoverComponent>(driver);

        vehicleComp.Driver = null;

        if (vehicleComp.HornAction != null)
            _actions.RemoveAction(driver, vehicleComp.HornAction);

        if (vehicleComp.SirenAction != null)
            _actions.RemoveAction(driver, vehicleComp.SirenAction);

        _virtualItem.DeleteInHandsMatching(driver, vehicle);
    }

    private void OnGetAdditionalAccess(EntityUid uid, VehicleComponent component, ref GetAdditionalAccessEvent args)
    {
        if (component.Driver == null)
            return;

        args.Entities.Add(component.Driver.Value);
    }
}

public sealed partial class HornActionEvent : InstantActionEvent;

public sealed partial class SirenActionEvent : InstantActionEvent;
