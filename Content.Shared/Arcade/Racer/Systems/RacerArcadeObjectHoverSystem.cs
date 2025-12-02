using Content.Shared.Arcade.Racer.Components;
using Content.Shared.Arcade.Racer.Events;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Arcade.Racer.Systems;

public sealed partial class RacerArcadeObjectHoverSystem : EntitySystem
{
    [Dependency] private readonly RacerArcadeObjectCollisionSystem _collision = default!;
    [Dependency] private readonly RacerArcadeObjectPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RacerArcadeObjectHoverComponent, RacerArcadeObjectPhysicsGetVelocityEvent>(OnGetVelocity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RacerArcadeObjectHoverComponent, RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent>();
        while (query.MoveNext(out var uid, out var hover, out var physics, out var data))
        {
            if (!TryGetHeightError((uid, hover, data), out var heightError))
                continue;

            if (MathHelper.CloseToPercent(heightError.Value, RacerArcadeObjectHoverComponent.HeightErrorThreshold))
                _physics.UpdateVelocity((uid, physics));
        }
    }

    private void OnGetVelocity(Entity<RacerArcadeObjectHoverComponent> ent, ref RacerArcadeObjectPhysicsGetVelocityEvent args)
    {
        if (!TryGetHeightError((ent.Owner, ent.Comp), out var heightError))
            return;

        var physics = Comp<RacerArcadeObjectPhysicsComponent>(ent.Owner);

        var spring = ent.Comp.Strength * heightError.Value;
        var damping = ent.Comp.Damping * physics.Velocity.Z;
        var accel = spring - damping;
        args.Velocity += new Vector3(0f, 0f, accel);
    }

    private bool TryGetHeightError(Entity<RacerArcadeObjectHoverComponent, RacerArcadeObjectComponent?> ent, [NotNullWhen(true)] out float? heightError)
    {
        if (!_collision.TryGetTrackHeightAtPosition((ent.Owner, ent.Comp2), out var trackHeight))
        {
            heightError = null;
            return false;
        }

        var desieredHeight = trackHeight + ent.Comp1.Distance;
        heightError = desieredHeight - ent.Comp2!.Position.Z;
        return true;
    }
}
