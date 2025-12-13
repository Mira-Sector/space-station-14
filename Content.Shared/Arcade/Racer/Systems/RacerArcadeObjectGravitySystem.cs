using Content.Shared.Arcade.Racer.Components;
using Content.Shared.Arcade.Racer.Events;

namespace Content.Shared.Arcade.Racer.Systems;

public sealed partial class RacerArcadeObjectGravitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RacerArcadeObjectGravityComponent, RacerArcadeObjectPhysicsGetForcesEvent>(OnGetForces);
    }

    private void OnGetForces(Entity<RacerArcadeObjectGravityComponent> ent, ref RacerArcadeObjectPhysicsGetForcesEvent args)
    {
        var physics = Comp<RacerArcadeObjectPhysicsComponent>(ent.Owner);
        var gravityForce = ent.Comp.Acceleration * physics.Mass;
        args.Force += new Vector3(0f, 0f, gravityForce);
    }
}
