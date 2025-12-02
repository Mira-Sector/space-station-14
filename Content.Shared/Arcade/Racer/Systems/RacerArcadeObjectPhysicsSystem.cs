using Content.Shared.Arcade.Racer.Components;
using Content.Shared.Arcade.Racer.Events;
using JetBrains.Annotations;

namespace Content.Shared.Arcade.Racer.Systems;

public sealed partial class RacerArcadeObjectPhysicsSystem : EntitySystem
{
    private EntityQuery<RacerArcadeObjectPhysicsComponent> _physics;

    public override void Initialize()
    {
        base.Initialize();

        _physics = GetEntityQuery<RacerArcadeObjectPhysicsComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent>();
        while (query.MoveNext(out var uid, out var physics, out var data))
        {
            if (!MathHelper.CloseToPercent(physics.Velocity.LengthSquared, 0f))
            {
                data.Position += physics.Velocity * frameTime;
                DirtyField(uid, data, nameof(RacerArcadeObjectComponent.Position));
            }

            if (!MathHelper.CloseToPercent(physics.AngularVelocity.LengthSquared, 0f))
            {
                var deltaAngle = physics.AngularVelocity * frameTime;
                var angle = deltaAngle.Length;

                if (!MathHelper.CloseTo(angle, 0f))
                {
                    var axis = deltaAngle / angle;
                    var rotation = Quaternion.FromAxisAngle(axis, angle);

                    data.Rotation = Quaternion.Normalize(rotation * data.Rotation);
                    DirtyField(uid, data, nameof(RacerArcadeObjectComponent.Rotation));
                }
            }
        }
    }

    [PublicAPI]
    public void UpdateVelocity(Entity<RacerArcadeObjectPhysicsComponent?> ent)
    {
        if (!_physics.Resolve(ent.Owner, ref ent.Comp))
            return;

        var ev = new RacerArcadeObjectPhysicsGetVelocityEvent();
        RaiseLocalEvent(ent.Owner, ref ev);

        ent.Comp.Velocity = ev.Velocity;
        ent.Comp.AngularVelocity = ev.AngularVelocity;

        DirtyField(ent.Owner, ent.Comp, nameof(RacerArcadeObjectPhysicsComponent.Velocity));
        DirtyField(ent.Owner, ent.Comp, nameof(RacerArcadeObjectPhysicsComponent.AngularVelocity));
    }

}
