using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands;
using Content.Server.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Vehicles;

namespace Content.Server.Vehicles;

public sealed class VehicleSystem : SharedVehicleSystem
{
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly VirtualItemSystem _virtualItem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VehicleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VehicleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<VehicleComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<VehicleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<VehicleComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<VehicleComponent, VirtualItemDeletedEvent>(OnDropped);
    }

    private void OnInit(EntityUid uid, VehicleComponent component, ComponentInit args)
    {

    }

    private void OnRemove(EntityUid uid, VehicleComponent component, ComponentRemove args)
    {
        if (component.Driver == null)
            return;

        _buckle.Unbuckle(component.Driver.Value, component.Driver.Value);
        Dismount(component.Driver.Value, uid);
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
    }

    private void OnStrapped(Entity<VehicleComponent> ent, ref StrappedEvent args)
    {
        var driver = args.Buckle.Owner;

        if (!TryComp(driver, out MobMoverComponent? mover))
            return;

        if (ent.Comp.Driver != null)
            return;

        ent.Comp.Driver = driver;

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

        vehicleComp.Driver = null;

        _virtualItem.DeleteInHandsMatching(driver, vehicle);

        return true;
    }
}
