using Content.Shared.Buckle.Components;
using Content.Shared.DrawDepth;
using Content.Shared.Vehicles;
using Robust.Client.GameObjects;

namespace Content.Server.Vehicles;

public sealed class VehicleSystem : SharedVehicleSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VehicleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<VehicleComponent, UnstrappedEvent>(OnUnstrapped);
    }

    private void OnStrapped(Entity<VehicleComponent> ent, ref StrappedEvent args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var spriteComp))
            return;

        spriteComp.DrawDepth = (int)Content.Shared.DrawDepth.DrawDepth.OverMobs;
    }

    private void OnUnstrapped(Entity<VehicleComponent> ent, ref UnstrappedEvent args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var spriteComp))
            return;

        spriteComp.DrawDepth = (int)Content.Shared.DrawDepth.DrawDepth.Objects;
    }
}
