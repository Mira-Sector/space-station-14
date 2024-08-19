using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Vehicles;

namespace Content.Shared.Vehicles;

public abstract partial class SharedVehicleSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        //SubscribeLocalEvent<VehicleComponent, StrappedEvent>(OnStrapped);
        //SubscribeLocalEvent<VehicleComponent, UnstrappedEvent>(OnUnstrapped);
    }

    private void OnStrapped(Entity<VehicleComponent> ent, ref StrappedEvent args)
    {
        //EnsureComp<RelayInputMoverComponent>(args.Buckle, out var relayComp);
        //relayComp.RelayEntity = ent.Owner;
    }

    private void OnUnstrapped(Entity<VehicleComponent> ent, ref UnstrappedEvent args)
    {
        //RemComp<RelayInputMoverComponent>(args.Buckle);
    }
}
