using Content.Shared.Arcade.Racer.Components;
using Content.Shared.Arcade.Racer.Events;

namespace Content.Shared.Arcade.Racer.Systems;

public sealed partial class RacerArcadeObjectPhysicsSystem : EntitySystem
{
    [Dependency] private readonly RacerArcadeObjectCollisionSystem _collision = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesBefore.Add(typeof(RacerArcadeObjectCollisionSystem));

        SubscribeLocalEvent<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectStartCollisionWithTrackEvent>(OnStartCollideWithTrack);
        SubscribeLocalEvent<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectActiveCollisionWithTrackEvent>(OnActiveCollideWithTrack);
    }

    private void OnStartCollideWithTrack(Entity<RacerArcadeObjectPhysicsComponent> ent, ref RacerArcadeObjectStartCollisionWithTrackEvent args)
    {
        if (args.Penetration < 0f)
            return;

        var vn = Vector3.Dot(ent.Comp.Velocity, args.Normal);
        if (vn < 0f)
        {
            ent.Comp.Velocity -= vn * args.Normal * (1f + ent.Comp.Restitution);
            DirtyField(ent.Owner, ent.Comp, nameof(RacerArcadeObjectPhysicsComponent.Velocity));
        }
    }

    private void OnActiveCollideWithTrack(Entity<RacerArcadeObjectPhysicsComponent> ent, ref RacerArcadeObjectActiveCollisionWithTrackEvent args)
    {
        if (args.Penetration < 0f)
            return;

        ent.Comp.PendingPositionCorrection += args.Normal * args.Penetration;

        var vn = Vector3.Dot(ent.Comp.Velocity, args.Normal);
        if (vn < 0f)
        {
            ent.Comp.Velocity -= vn * args.Normal; // no restitution as this is just corrective
            DirtyField(ent.Owner, ent.Comp, nameof(RacerArcadeObjectPhysicsComponent.Velocity));
        }
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

            ApplyPredictedPosition((uid, physics, data), frameTime);
            ApplyPredictedRotation((uid, physics, data), frameTime);

            CancelForces((uid, physics, data));

            ApplyAcceleration((uid, physics), frameTime);

            ApplyPosition((uid, physics, data), frameTime);
            ApplyRotation((uid, physics, data), frameTime);

            ApplyCorrection((uid, physics, data));

            physics.PendingPositionCorrection = Vector3.Zero;
            physics.PendingRotationCorrection = Quaternion.Identity;

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
        ent.Comp.Velocity += GetPositionAcceleration(ent) * frameTime;

        ent.Comp.AngularVelocity += GetAngularAcceleration(ent) * frameTime;
    }

    private static void ApplyPredictedPosition(Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> ent, float frameTime)
    {
        var acceleration = GetPositionAcceleration(ent);
        ent.Comp1.PredictedPosition = ent.Comp2.Position + ent.Comp1.Velocity * frameTime + 0.5f * acceleration * frameTime * frameTime;
    }

    private static void ApplyPredictedRotation(Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> ent, float frameTime)
    {
        if (MathHelper.CloseToPercent(ent.Comp1.AngularVelocity.LengthSquared, 0f))
        {
            ent.Comp1.PredictedRotation = ent.Comp2.Rotation;
            return;
        }

        var angularAccel = GetAngularAcceleration(ent) * frameTime;
        var deltaAngleFromVelocity = ent.Comp1.AngularVelocity * frameTime;

        var totalDeltaAngle = deltaAngleFromVelocity + 0.5f * angularAccel * frameTime;

        var angle = totalDeltaAngle.Length;
        if (MathHelper.CloseTo(angle, 0f))
        {
            ent.Comp1.PredictedRotation = ent.Comp2.Rotation;
            return;
        }

        var axis = totalDeltaAngle / angle;
        var rotation = Quaternion.FromAxisAngle(axis, angle);
        ent.Comp1.PredictedRotation = Quaternion.Normalize(rotation * ent.Comp2.Rotation);
    }

    private void CancelForces(Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> ent)
    {
        var contacts = _collision.GetPredictedCollisionContacts((ent.Owner, null, ent.Comp2), ent.Comp1.PredictedPosition, ent.Comp1.PredictedRotation);
        foreach (var contact in contacts)
        {
            // cancel the component of the force pushing into the collision
            //
            var velocityNormal = Vector3.Dot(ent.Comp1.Velocity, contact.Normal);
            if (velocityNormal < 0f)
                ent.Comp1.Velocity -= velocityNormal * contact.Normal;

            var angularNormal = Vector3.Dot(ent.Comp1.AngularVelocity, contact.Normal);
            if (angularNormal < 0f)
                ent.Comp1.AngularVelocity -= angularNormal * contact.Normal;
        }
    }

    private static void ApplyPosition(Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> ent, float frameTime)
    {
        ent.Comp2.Position += ent.Comp1.Velocity * frameTime;
    }

    private static void ApplyRotation(Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> ent, float frameTime)
    {
        if (MathHelper.CloseToPercent(ent.Comp1.AngularVelocity.LengthSquared, 0f))
            return;

        var angularAccel = GetAngularAcceleration(ent) * frameTime;
        var deltaAngleFromVelocity = ent.Comp1.AngularVelocity * frameTime;

        var totalDeltaAngle = deltaAngleFromVelocity + 0.5f * angularAccel * frameTime;

        var angle = totalDeltaAngle.Length;
        if (MathHelper.CloseTo(angle, 0f))
            return;

        var axis = totalDeltaAngle / angle;
        var rotation = Quaternion.FromAxisAngle(axis, angle);
        ent.Comp2.Rotation = Quaternion.Normalize(rotation * ent.Comp2.Rotation);
    }

    private static void ApplyCorrection(Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> ent)
    {
        ent.Comp2.Position += ent.Comp1.PendingPositionCorrection;
        ent.Comp2.Rotation *= ent.Comp1.PendingRotationCorrection;
    }

    private static Vector3 GetPositionAcceleration(Entity<RacerArcadeObjectPhysicsComponent> ent)
    {
        return ent.Comp.AccumulatedForce / ent.Comp.Mass;
    }

    private static Vector3 GetAngularAcceleration(Entity<RacerArcadeObjectPhysicsComponent> ent)
    {
        return ent.Comp.AccumulatedTorque / ent.Comp.MomentOfInertia;
    }
}
