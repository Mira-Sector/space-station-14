using Content.Shared.Arcade.Racer.Components;
using Content.Shared.Arcade.Racer.CollisionShapes;
using Content.Shared.Arcade.Racer.Events;
using Content.Shared.Arcade.Racer.Stage;
using Content.Shared.Maths;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Content.Shared.Arcade.Racer.Systems;

public sealed partial class RacerArcadeObjectCollisionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedRacerArcadeSystem _racer = default!;

    private EntityQuery<RacerArcadeObjectComponent> _data;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RacerArcadeObjectCollisionComponent, ComponentInit>(OnInit);

        _data = GetEntityQuery<RacerArcadeObjectComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        HashSet<(EntityUid, EntityUid)> handledPairs = [];
        var query = EntityQueryEnumerator<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent>();
        while (query.MoveNext(out var uid, out var collision, out var data))
            HandleCollisions((uid, collision, data), handledPairs);
    }

    private void OnInit(Entity<RacerArcadeObjectCollisionComponent> ent, ref ComponentInit args)
    {
        var data = _data.Get(ent.Owner);
        UpdateCachedAABB((ent.Owner, ent.Comp, data));
        UpdateCachedCollisionFlags(ent);
    }

    private void HandleCollisions(Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent> ent, HashSet<(EntityUid, EntityUid)> handledPairs)
    {
        HandleEntityCollisions(ent, handledPairs);
        HandleTrackCollisions(ent);
    }

    private void HandleEntityCollisions(Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent> ent, HashSet<(EntityUid, EntityUid)> handledPairs)
    {
        if (!_racer.TryGetArcade((ent.Owner, ent.Comp2), out var ourArcade))
            return;

        var ourAABB = ent.Comp1.CachedAABB;

        var query = EntityQueryEnumerator<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent>();
        while (query.MoveNext(out var uid, out var physics, out var data))
        {
            Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent> other = new(uid, physics, data);
            if (other.Owner == ent.Owner)
                continue;

            /*
             * so we only store one pair
             * and handle the inverse case
             * where other goes through the loop and finds has the inverse pair
             * this obviously would fail the contains
            */
            var pair = ent.Owner.Id < other.Owner.Id
                ? (ent.Owner, other.Owner)
                : (other.Owner, ent.Owner);
            if (handledPairs.Contains(pair))
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

            var ourEv = new RacerArcadeObjectCollisionWithObjectEvent(other.Owner, shapeIds.Value.aId, shapeIds.Value.bId);
            RaiseLocalEvent(ent.Owner, ref ourEv);

            var otherEv = new RacerArcadeObjectCollisionWithObjectEvent(ent.Owner, shapeIds.Value.bId, shapeIds.Value.aId);
            RaiseLocalEvent(other.Owner, ref otherEv);

            handledPairs.Add(pair);
        }
    }

    private void HandleTrackCollisions(Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent> ent)
    {
        if (!_racer.TryGetArcade((ent.Owner, ent.Comp2), out var arcade))
            return;

        var ourAABB = ent.Comp1.CachedAABB;

        if (arcade.Value.Comp.State is not { } state)
            return;

        if ((ent.Comp1.AllMasks & RacerArcadeStageGraph.CollisionLayer) == 0 || (RacerArcadeStageGraph.CollisionMask & ent.Comp1.AllLayers) == 0)
            return;

        var stage = _prototype.Index(state.CurrentStage);
        if (!stage.Graph.AABB.Intersects(ourAABB))
            return;

        if (!CheckCollidingShapes(ent.Comp1.Shapes, stage.Graph.CollisionShapes, out var shapeIds))
            return;

        var box = shapeIds.Value.bEntry.Shape.GetBox();
        var normal = Vector3.Transform(Vector3.UnitZ, box.Quaternion);
        var contactHeight = ent.Comp2.Position.Z - Vector3.Dot(ent.Comp2.Position - box.Origin, normal);

        var ev = new RacerArcadeObjectCollisionWithTrackEvent(shapeIds.Value.aId, contactHeight, normal);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    private void UpdateCachedAABB(Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent> ent)
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
        DirtyField(ent.Owner, ent.Comp1, nameof(RacerArcadeObjectCollisionComponent.CachedAABB));
    }

    private void UpdateCachedCollisionFlags(Entity<RacerArcadeObjectCollisionComponent> ent)
    {
        ent.Comp.AllLayers = (int)RacerArcadeCollisionGroups.None;
        ent.Comp.AllMasks = (int)RacerArcadeCollisionGroups.None;

        foreach (var entry in ent.Comp.Shapes.Values)
        {
            ent.Comp.AllLayers |= entry.Layer;
            ent.Comp.AllMasks |= entry.Mask;
        }

        DirtyField(ent.Owner, ent.Comp, nameof(RacerArcadeObjectCollisionComponent.AllLayers));
        DirtyField(ent.Owner, ent.Comp, nameof(RacerArcadeObjectCollisionComponent.AllMasks));
    }

    private static bool CheckCollidingShapes(
        Dictionary<string, RacerArcadeCollisionShapeEntry> a,
        Dictionary<string, RacerArcadeCollisionShapeEntry> b,
        [NotNullWhen(true)] out (string aId, RacerArcadeCollisionShapeEntry aEntry, string bId, RacerArcadeCollisionShapeEntry bEntry)? shapes)
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

                if (!RacerArcadeObjectCollisionResolver.Resolve(aEntry.Shape, bEntry.Shape))
                    continue;

                shapes = (aId, aEntry, bId, bEntry);
                return true;
            }
        }

        shapes = null;
        return false;
    }

    private static bool CheckCollidingShapes(
        Dictionary<string, RacerArcadeCollisionShapeEntry> a,
        List<RacerArcadeCollisionShapeEntry> b,
        [NotNullWhen(true)] out (string aId, RacerArcadeCollisionShapeEntry aEntry, RacerArcadeCollisionShapeEntry bEntry)? shape)
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

                if (!RacerArcadeObjectCollisionResolver.Resolve(aEntry.Shape, bEntry.Shape))
                    continue;

                shape = (aId, aEntry, bEntry);
                return true;
            }
        }

        shape = null;
        return false;
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

        foreach (var entry in stage.Graph.CollisionShapes)
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
