using Content.Shared.Arcade.Racer.Components;
using Content.Shared.Arcade.Racer.Events;
using Content.Shared.Arcade.Racer.PhysShapes;
using Content.Shared.Arcade.Racer.Stage;
using Content.Shared.Maths;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Content.Shared.Arcade.Racer.Systems;

public sealed partial class RacerArcadeObjectPhysicsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
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
        UpdateCachedPhysicsFlags(ent);
    }

    private void HandleCollisions(Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> ent)
    {
        HandleEntityCollisions(ent);
        HandleTrackCollisions(ent);
    }

    private void HandleEntityCollisions(Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> ent)
    {
        if (!_racer.TryGetArcade((ent.Owner, ent.Comp2), out var ourArcade))
            return;

        var ourAABB = ent.Comp1.CachedAABB;

        var query = EntityQueryEnumerator<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent>();
        while (query.MoveNext(out var uid, out var physics, out var data))
        {
            Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> other = new(uid, physics, data);
            if (other.Owner == ent.Owner)
                continue;

            if (!_racer.TryGetArcade((other.Owner, other.Comp2), out var otherArcade))
                continue;

            if (ourArcade != otherArcade)
                continue;

            if ((ent.Comp1.AllMasks & other.Comp1.AllLayers) == 0 || (other.Comp1.AllMasks & ent.Comp1.AllLayers) == 0)
                continue;

            var otherAABB = other.Comp1.CachedAABB;
            if (!ourAABB.Intersects(otherAABB))
                continue;

            if (!CheckCollidingShapes(ent.Comp1.Shapes, other.Comp1.Shapes, out var shapeIds))
                continue;
        }
    }

    private void HandleTrackCollisions(Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> ent)
    {
        if (!_racer.TryGetArcade((ent.Owner, ent.Comp2), out var arcade))
            return;

        var ourAABB = ent.Comp1.CachedAABB;

        if (arcade.Value.Comp.State is not { } state)
            return;

        if ((ent.Comp1.AllMasks & RacerArcadeStageGraph.PhysicsLayer) == 0 || (RacerArcadeStageGraph.PhysicsMask & ent.Comp1.AllLayers) == 0)
            return;

        var stage = _prototype.Index(state.CurrentStage);
        if (!stage.Graph.AABB.Intersects(ourAABB))
            return;

        if (!CheckCollidingShapes(ent.Comp1.Shapes, stage.Graph.PhysicsShapes, out var shapeId))
            return;
    }

    private void UpdateCachedAABB(Entity<RacerArcadeObjectPhysicsComponent, RacerArcadeObjectComponent> ent)
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
    }

    private void UpdateCachedPhysicsFlags(Entity<RacerArcadeObjectPhysicsComponent> ent)
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
        Dictionary<string, RacerArcadePhysicsShapeEntry> a,
        Dictionary<string, RacerArcadePhysicsShapeEntry> b,
        [NotNullWhen(true)] out (string a, string b)? shapeIds)
    {
        foreach (var (aId, aEntry) in a)
        {
            var aBox = aEntry.Shape.GetBox().CalcBoundingBox();

            foreach (var (bId, bEntry) in b)
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

    private static bool CheckCollidingShapes(
        Dictionary<string, RacerArcadePhysicsShapeEntry> a,
        List<RacerArcadePhysicsShapeEntry> b,
        [NotNullWhen(true)] out string? shapeId)
    {
        foreach (var (aId, aEntry) in a)
        {
            var aBox = aEntry.Shape.GetBox().CalcBoundingBox();

            foreach (var bEntry in b)
            {
                if ((aEntry.Mask & bEntry.Layer) == 0 || (bEntry.Mask & aEntry.Layer) == 0)
                    continue;

                var bBox = bEntry.Shape.GetBox().CalcBoundingBox();

                if (!aBox.Intersects(bBox))
                    continue;

                if (!RacerArcadeObjectPhysCollisionResolver.Resolve(aEntry.Shape, bEntry.Shape))
                    continue;

                shapeId = aId;
                return true;
            }
        }

        shapeId = null;
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

    [PublicAPI]
    public bool TryGetTrackHeightAtPosition(Entity<RacerArcadeObjectComponent?> ent, [NotNullWhen(true)] out float? height)
    {
        if (!_data.Resolve(ent.Owner, ref ent.Comp) || !_racer.TryGetArcade(ent, out var arcade))
        {
            height = null;
            return false;
        }

        return TryGetTrackHeightAtPosition(ent.Comp.Position, arcade.Value, out height);
    }

    [PublicAPI]
    public bool TryGetTrackHeightAtPosition(Vector3 position, Entity<RacerArcadeComponent> arcade, [NotNullWhen(true)] out float? height)
    {
        if (arcade.Comp.State is not { } state)
        {
            height = null;
            return false;
        }

        var stage = _prototype.Index(state.CurrentStage);

        foreach (var entry in stage.Graph.PhysicsShapes)
        {
            var shape = entry.Shape;
            var box = shape.GetBox();
            var aabb = box.CalcBoundingBox();

            if (!aabb.Contains(position))
                continue;

            var normal = Vector3.Transform(Vector3.UnitZ, box.Quaternion);
            height = position.Z - Vector3.Dot(position - box.Origin, normal);
            return true;
        }

        height = null;
        return false;
    }
}
