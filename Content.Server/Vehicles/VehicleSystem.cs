using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Vehicles;

namespace Content.Server.Vehicles;

public sealed class VehicleSystem : SharedVehicleSystem
{
    [Dependency] private readonly SharedMoverController _mover = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VehicleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<VehicleComponent, UnstrappedEvent>(OnUnstrapped);
    }

    private void OnStrapped(Entity<VehicleComponent> ent, ref StrappedEvent args)
    {
        if (!TryComp(args.Buckle.Owner, out MobMoverComponent? mover))
            return;

        if (ent.Comp.Driver != null)
            return;

        _mover.SetRelay(args.Buckle.Owner, ent.Owner);
        ent.Comp.Driver = args.Buckle.Owner;

    }
    private void OnUnstrapped(Entity<VehicleComponent> ent, ref UnstrappedEvent args)
    {
        if (ent.Comp.Driver != args.Buckle.Owner)
            return;

        RemComp<RelayInputMoverComponent>(ent.Comp.Driver.Value);
        ent.Comp.Driver = null;
    }
}
