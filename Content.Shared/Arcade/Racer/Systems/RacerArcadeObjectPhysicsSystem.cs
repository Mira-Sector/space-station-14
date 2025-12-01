using Content.Shared.Arcade.Racer.Components;
using Content.Shared.Arcade.Racer.Events;
using Content.Shared.Arcade.Racer.PhysShapes;
using Content.Shared.Maths;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Arcade.Racer.Systems;

public sealed partial class RacerArcadeObjectPhysicsSystem : EntitySystem
{
    [Dependency] private readonly SharedRacerArcadeSystem _racer = default!;

    private EntityQuery<RacerArcadeObjectComponent> _data;
    private EntityQuery<RacerArcadeObjectPhysicsComponent> _physics;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RacerArcadeObjectPhysicsComponent, ComponentInit>(OnInit);

        _physics = GetEntityQuery<RacerArcadeObjectPhysicsComponent>();
        _data = GetEntityQuery<RacerArcadeObjectComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent>();
        while (query.MoveNext(out var uid, out var physics, out var data))
        {
            HandleCollisions((uid, physics, data));

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

    private void OnInit(Entity<RacerArcadeObjectPhysicsComponent> ent, ref ComponentInit args)
    {
        var data = _data.Get(ent.Owner);
        UpdateCachedAABB((ent.Owner, ent.Comp, data));
        UpdateCachedShapePhysicsFlags(ent);
    }

    private void HandleCollisions(Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> ent)
    {
        var ourArcade = _racer.GetArcade((ent.Owner, ent.Comp2));
        var ourAABB = ent.Comp1.CachedAABB;

        var query = EntityQueryEnumerator<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent>();
        while (query.MoveNext(out var uid, out var physics, out var data))
        {
            Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> other = new(uid, physics, data);
            if (other.Owner == ent.Owner)
                continue;

            var otherArcade = _racer.GetArcade((other.Owner, other.Comp2));
            if (ourArcade != otherArcade)
                continue;

            if ((ent.Comp1.AllMasks & other.Comp1.AllLayers) == 0 || (other.Comp1.AllMasks & ent.Comp1.AllLayers) == 0)
                continue;

            var otherAABB = other.Comp1.CachedAABB;
            if (!ourAABB.Intersects(otherAABB))
                continue;

            if (!CheckCollidingShapes(ent, other, out var shapeIds))
                continue;
        }
    }

    private Box3 UpdateCachedAABB(Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> ent)
    {
        var box = Box3Rotated.Empty;
        foreach (var entry in ent.Comp1.Shapes.Values)
        {
            var shapeBox = entry.Shape.GetBox();
            var shapeAABB = shapeBox.CalcBoundingBox();

            box = new Box3(
                Vector3.ComponentMin(box.LeftBottomBack, shapeAABB.LeftBottomBack),
                Vector3.ComponentMax(box.RightTopFront, shapeAABB.RightTopFront)
            );
        }
        box.Translate(ent.Comp2.Position);
        box.Rotate(ent.Comp2.Rotation);
        var aabb = box.CalcBoundingBox();
        ent.Comp1.CachedAABB = aabb;
        DirtyField(ent.Owner, ent.Comp1, nameof(RacerArcadeObjectPhysicsComponent.CachedAABB));
        return aabb;
    }

    private void UpdateCachedShapePhysicsFlags(Entity<RacerArcadeObjectPhysicsComponent> ent)
    {
        ent.Comp.AllLayers = (int)RacerArcadePhysicsGroups.None;
        ent.Comp.AllMasks = (int)RacerArcadePhysicsGroups.None;

        foreach (var entry in ent.Comp.Shapes.Values)
        {
            ent.Comp.AllLayers |= entry.Layer;
            ent.Comp.AllMasks |= entry.Mask;
        }

        DirtyField(ent.Owner, ent.Comp, nameof(RacerArcadeObjectPhysicsComponent.AllLayers));
        DirtyField(ent.Owner, ent.Comp, nameof(RacerArcadeObjectPhysicsComponent.AllMasks));
    }

    private static bool CheckCollidingShapes(
        Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> a,
        Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> b,
        [NotNullWhen(true)] out (string a, string b)? shapeIds)
    {
        foreach (var (aId, aEntry) in a.Comp1.Shapes)
        {
            var aBox = aEntry.Shape.GetBox().CalcBoundingBox();

            foreach (var (bId, bEntry) in b.Comp1.Shapes)
            {
                if ((aEntry.Mask & bEntry.Layer) == 0 || (bEntry.Mask & aEntry.Layer) == 0)
                    continue;

                var bBox = bEntry.Shape.GetBox().CalcBoundingBox();

                if (!aBox.Intersects(bBox))
                    continue;

                if (!RacerArcadeObjectPhysCollisionResolver.Resolve(aEntry.Shape, bEntry.Shape))
                    continue;

                shapeIds = (aId, bId);
                return true;
            }
        }

        shapeIds = null;
        return false;
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
