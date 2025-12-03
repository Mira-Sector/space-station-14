using Content.Shared.Arcade.Racer.Components;
using Content.Shared.Arcade.Racer.Events;

namespace Content.Shared.Arcade.Racer.Systems;

public sealed partial class RacerArcadeObjectHoverSystem : EntitySystem
{
    [Dependency] private readonly RacerArcadeObjectCollisionSystem _collision = default!;
    [Dependency] private readonly SharedRacerArcadeSystem _racer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RacerArcadeObjectHoverComponent, RacerArcadeObjectPhysicsGetForcesEvent>(OnGetForces);
    }

    private void OnGetForces(Entity<RacerArcadeObjectHoverComponent> ent, ref RacerArcadeObjectPhysicsGetForcesEvent args)
    {
        var data = _racer.GetData(ent.Owner);

        if (!_collision.TryGetTrackHeightAtPosition((ent.Owner, data), out var trackHeight))
            return;

        var physics = Comp<RacerArcadeObjectPhysicsComponent>(ent.Owner);

        var targetHeight = trackHeight.Value + ent.Comp.TargetHeight;
        var heightError = targetHeight - data.Position.Z;

        var springForce = ent.Comp.Strength * heightError;
        var dampingForce = -ent.Comp.Damping * physics.Velocity.Z;

        var totalForce = springForce + dampingForce;
        totalForce = Math.Clamp(totalForce, -ent.Comp.MaxForce, ent.Comp.MaxForce);

        args.Force += new Vector3(0f, 0f, totalForce);
    }
}
