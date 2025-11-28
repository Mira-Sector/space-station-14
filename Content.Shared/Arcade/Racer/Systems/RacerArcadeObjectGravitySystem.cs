using Content.Shared.Arcade.Racer.Components;
using Content.Shared.Arcade.Racer.Events;

namespace Content.Shared.Arcade.Racer.Systems;

public sealed partial class RacerArcadeObjectGravitySystem : EntitySystem
{
    [Dependency] private readonly RacerArcadeObjectPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RacerArcadeObjectGravityComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RacerArcadeObjectGravityComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<RacerArcadeObjectGravityComponent, RacerArcadeObjectPhysicsGetVelocityEvent>(OnGetVelocity);
    }

    private void OnStartup(Entity<RacerArcadeObjectGravityComponent> ent, ref ComponentStartup args)
    {
        _physics.UpdateVelocity(ent.Owner);
    }

    private void OnShutdown(Entity<RacerArcadeObjectGravityComponent> ent, ref ComponentShutdown args)
    {
        _physics.UpdateVelocity(ent.Owner);
    }

    private void OnGetVelocity(Entity<RacerArcadeObjectGravityComponent> ent, ref RacerArcadeObjectPhysicsGetVelocityEvent args)
    {
        args.Velocity += ent.Comp.Force;
    }
}
