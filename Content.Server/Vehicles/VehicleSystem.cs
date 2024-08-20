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
        SubscribeLocalEvent<VehicleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<VehicleComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<VehicleComponent, VirtualItemDeletedEvent>(OnDropped);
    }

    private void OnInit(EntityUid uid, VehicleComponent component, ComponentInit args)
    {

    }

    private void OnRemove(EntityUid uid, VehicleComponent component, ComponentRemove args)
    {

    }

    private void OnStrapped(Entity<VehicleComponent> ent, ref StrappedEvent args)
    {
        if (!TryComp(args.Buckle.Owner, out MobMoverComponent? mover))
            return;

        if (ent.Comp.Driver != null)
            return;

        _mover.SetRelay(args.Buckle.Owner, ent.Owner);

        ent.Comp.Driver = args.Buckle.Owner;

        if (ent.Comp.RequiredHands == 0)
            return;

        for (int hands = 0; hands < ent.Comp.RequiredHands; hands++)
        {
            _virtualItem.TrySpawnVirtualItemInHand(ent.Owner, args.Buckle.Owner, true);
        }
    }

    private void OnUnstrapped(Entity<VehicleComponent> ent, ref UnstrappedEvent args)
    {
        Dismount(args.Buckle.Owner, ent);
    }

    private void OnDropped(EntityUid uid, VehicleComponent comp, VirtualItemDeletedEvent args)
    {
        if (!Dismount(args.User, comp))
            return;

        _buckle.TryUnbuckle(args.User, args.User);
    }

    private bool Dismount(EntityUid driver, VehicleComponent vehicle)
    {
        if (vehicle.Driver != driver)
            return false;

        if (!RemComp<RelayInputMoverComponent>(driver))
            return false;

        if (vehicle.RequiredHands != 0)
            _virtualItem.DeleteInHandsMatching(driver, vehicle.Owner);

        vehicle.Driver = null;

        return true;
    }
}
