using Content.Shared.Arcade.Racer.Components;
using Content.Shared.Arcade.Racer.Events;

namespace Content.Shared.Arcade.Racer.Systems;

public sealed partial class RacerArcadeObjectPhysicsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectCollisionWithTrackEvent>(OnCollideWithTrack);
    }

    private void OnCollideWithTrack(Entity<RacerArcadeObjectPhysicsComponent> ent, ref RacerArcadeObjectCollisionWithTrackEvent args)
    {
        ent.Comp.Velocity = new Vector3(ent.Comp.Velocity.X, ent.Comp.Velocity.Y, 0f);
        DirtyField(ent.Owner, ent.Comp, nameof(RacerArcadeObjectPhysicsComponent.Velocity));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent>();
        while (query.MoveNext(out var uid, out var physics, out var data))
        {
            physics.AccumulatedForce = Vector3.Zero;
            physics.AccumulatedTorque = Vector3.Zero;

            GetForces((uid, physics));
            ApplyDrag((uid, physics));

            ApplyAcceleration((uid, physics), frameTime);

            ApplyPosition((uid, physics, data), frameTime);
            ApplyRotation((uid, physics, data), frameTime);

            Dirty(uid, physics);
            Dirty(uid, data);
        }
    }

    private void GetForces(Entity<RacerArcadeObjectPhysicsComponent> ent)
    {
        var ev = new RacerArcadeObjectPhysicsGetForcesEvent();
        RaiseLocalEvent(ent.Owner, ref ev);

        ent.Comp.AccumulatedForce = ev.Force;
        ent.Comp.AccumulatedTorque = ev.Torque;
    }

    private static void ApplyDrag(Entity<RacerArcadeObjectPhysicsComponent> ent)
    {
        var dragForce = -ent.Comp.LinearDrag * ent.Comp.Velocity;
        ent.Comp.AccumulatedForce += dragForce;

        var angularDragTorque = -ent.Comp.AngularDrag * ent.Comp.AngularVelocity;
        ent.Comp.AccumulatedTorque += angularDragTorque;
    }

    private static void ApplyAcceleration(Entity<RacerArcadeObjectPhysicsComponent> ent, float frameTime)
    {
        var acceleration = ent.Comp.AccumulatedForce / ent.Comp.Mass;
        ent.Comp.Velocity += acceleration * frameTime;

        var angularAcceleration = ent.Comp.AccumulatedTorque / ent.Comp.MomentOfInertia;
        ent.Comp.AngularVelocity += angularAcceleration * frameTime;
    }

    private static void ApplyPosition(Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> ent, float frameTime)
    {
        ent.Comp2.Position += ent.Comp1.Velocity * frameTime;
    }

    private static void ApplyRotation(Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> ent, float frameTime)
    {
        if (MathHelper.CloseToPercent(ent.Comp1.AngularVelocity.LengthSquared, 0f))
            return;

        var deltaAngle = ent.Comp1.AngularVelocity * frameTime;
        var angle = deltaAngle.Length;

        if (MathHelper.CloseTo(angle, 0f))
            return;

        var axis = deltaAngle / angle;
        var rotation = Quaternion.FromAxisAngle(axis, angle);
        ent.Comp2.Rotation = Quaternion.Normalize(rotation * ent.Comp2.Rotation);
    }
}
