using Content.Shared.Actions;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vehicles;

public abstract partial class SharedVehicleSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;

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

        SubscribeLocalEvent<VehicleComponent, HornActionEvent>(OnHorn);
        SubscribeLocalEvent<VehicleComponent, SirenActionEvent>(OnSiren);
    }

    private void OnInit(EntityUid uid, VehicleComponent component, ComponentInit args)
    {
        _appearance.SetData(uid, VehicleState.Animated, false);
    }

    private void OnRemove(EntityUid uid, VehicleComponent component, ComponentRemove args)
    {
        if (component.Driver == null)
            return;

        _buckle.Unbuckle(component.Driver.Value, component.Driver.Value);
        Dismount(component.Driver.Value, uid);
    }

    private void OnHorn(EntityUid uid, VehicleComponent component, InstantActionEvent args)
    {
        if (component.Driver != args.Performer)
            return;

        _audio.PlayPvs(component.HornSound, component.Owner);
    }

    private void OnSiren(EntityUid uid, VehicleComponent component, InstantActionEvent args)
    {
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
            component.SirenStream = _audio.PlayPvs(component.SirenSound, component.Owner, AudioParams.Default.WithLoop(true).WithMaxDistance(5)).Value.Entity;
        }

        component.SirenEnabled = !component.SirenEnabled;
    }


    private void OnStrapAttempt(Entity<VehicleComponent> ent, ref StrapAttemptEvent args)
    {
        var driver = args.Buckle.Owner; // i dont want to re write this shit 100 fucking times

        if (ent.Comp.Driver != null)
            return;

        if (ent.Comp.RequiredHands == 0)
            return;

        for (int hands = 0; hands < ent.Comp.RequiredHands; hands++)
        {
            if (!_virtualItem.TrySpawnVirtualItemInHand(ent.Owner, driver, false))
            {
                args.Cancelled = true;
                _virtualItem.DeleteInHandsMatching(driver, ent.Owner);
                return;
            }
        }

        if (ent.Comp.HornSound != null)
            _actions.AddAction(driver, ref ent.Comp.HornAction, HornActionId, ent);

        if (ent.Comp.SirenSound != null)
            _actions.AddAction(driver, ref ent.Comp.SirenAction, SirenActionId, ent);
    }

    private void OnStrapped(Entity<VehicleComponent> ent, ref StrappedEvent args)
    {
        var driver = args.Buckle.Owner;

        if (!TryComp(driver, out MobMoverComponent? mover))
            return;

        if (ent.Comp.Driver != null)
            return;

        ent.Comp.Driver = driver;

        _appearance.SetData(ent.Owner, VehicleState.Animated, true);
        _mover.SetRelay(driver, ent.Owner);
    }

    private void OnUnstrapped(Entity<VehicleComponent> ent, ref UnstrappedEvent args)
    {
        if (ent.Comp.Driver != args.Buckle.Owner)
            return;

        Dismount(args.Buckle.Owner, ent);
    }

    private void OnDropped(EntityUid uid, VehicleComponent comp, VirtualItemDeletedEvent args)
    {
        if (comp.Driver != args.User)
            return;

        _buckle.Unbuckle(args.User, args.User);

        if (!Dismount(args.User, comp.Owner))
            return;
    }

    private bool Dismount(EntityUid driver, EntityUid vehicle)
    {
        if (!TryComp<VehicleComponent>(vehicle, out var vehicleComp))
            return false;

        if (vehicleComp.Driver != driver)
            return false;

        RemComp<RelayInputMoverComponent>(driver);

        _appearance.SetData(vehicle, VehicleState.Animated, false);
        vehicleComp.Driver = null;

        if (vehicleComp.HornAction != null)
            _actions.RemoveAction(driver, vehicleComp.HornAction);

        if (vehicleComp.SirenAction != null)
            _actions.RemoveAction(driver, vehicleComp.SirenAction);

        _virtualItem.DeleteInHandsMatching(driver, vehicle);

        return true;
    }
}

public sealed partial class HornActionEvent : InstantActionEvent;

public sealed partial class SirenActionEvent : InstantActionEvent;
