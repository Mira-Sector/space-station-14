using Content.Shared.Arcade.Racer.Components;
using Content.Shared.Arcade.Racer.CollisionShapes;
using Content.Shared.Arcade.Racer.Events;
using Content.Shared.Arcade.Racer.Stage;
using Content.Shared.Maths;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using System.Linq;

namespace Content.Shared.Arcade.Racer.Systems;

public sealed partial class RacerArcadeObjectCollisionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedRacerArcadeSystem _racer = default!;

    private EntityQuery<RacerArcadeObjectCollisionComponent> _collision;
    private EntityQuery<RacerArcadeObjectComponent> _data;

    private struct CollisionEvents()
    {
        public List<(EntityUid, RacerArcadeObjectCollisionWithObjectEvent)> Object = [];
        public List<(EntityUid, RacerArcadeObjectCollisionWithTrackEvent)> Track = [];
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RacerArcadeObjectCollisionComponent, ComponentInit>(OnInit);

        _collision = GetEntityQuery<RacerArcadeObjectCollisionComponent>();
        _data = GetEntityQuery<RacerArcadeObjectComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        HashSet<(EntityUid, EntityUid)> handledPairs = [];
        CollisionEvents events = new();
        var query = EntityQueryEnumerator<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent>();
        while (query.MoveNext(out var uid, out var collision, out var data))
        {
            var newEvents = HandleCollisions((uid, collision, data), handledPairs);
            events.Object.AddRange(newEvents.Object);
            events.Track.AddRange(newEvents.Track);
        }

        /*
         * raise events after collision detection
         * prevent subscribers messing up further collisions
        */
        foreach (var (uid, ev) in events.Object)
        {
            var refEv = ev;
            RaiseLocalEvent(uid, ref refEv);
        }

        foreach (var (uid, ev) in events.Track)
        {
            var refEv = ev;
            RaiseLocalEvent(uid, ref refEv);
        }
    }

    private void OnInit(Entity<RacerArcadeObjectCollisionComponent> ent, ref ComponentInit args)
    {
        var data = _data.Get(ent.Owner);
        UpdateCachedAABB((ent.Owner, ent.Comp, data));
        UpdateCachedCollisionFlags(ent);
    }

    private CollisionEvents HandleCollisions(Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent> ent, HashSet<(EntityUid, EntityUid)> handledPairs)
    {
        return new()
        {
            Object = HandleEntityCollisions(ent, handledPairs).ToList(),
            Track = HandleTrackCollisions(ent).ToList()
        };
    }

    private IEnumerable<(EntityUid, RacerArcadeObjectCollisionWithObjectEvent)> HandleEntityCollisions(Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent> ent, HashSet<(EntityUid, EntityUid)> handledPairs)
    {
        if (!_racer.TryGetArcade((ent.Owner, ent.Comp2), out var ourArcade))
            yield break;

        var ourAABB = GetAABB(ent!);

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

            var otherAABB = GetAABB(other!);
            if (!ourAABB.Intersects(otherAABB))
                continue;

            foreach (var (aId, aEntry, bId, bEntry) in GetCollidingShapes(ent.Comp1.Shapes, other.Comp1.Shapes))
            {
                var ourEv = new RacerArcadeObjectCollisionWithObjectEvent(other.Owner, aId, bId);
                yield return (ent.Owner, ourEv);

                var otherEv = new RacerArcadeObjectCollisionWithObjectEvent(ent.Owner, bId, aId);
                yield return (other.Owner, otherEv);
            }

            handledPairs.Add(pair);
        }
    }

    private IEnumerable<(EntityUid, RacerArcadeObjectCollisionWithTrackEvent)> HandleTrackCollisions(Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent> ent)
    {
        if (!_racer.TryGetArcade((ent.Owner, ent.Comp2), out var arcade))
            yield break;

        var ourAABB = ent.Comp1.CachedAABB;

        if (arcade.Value.Comp.State is not { } state)
            yield break;

        if ((ent.Comp1.AllMasks & RacerArcadeStageGraph.CollisionLayer) == 0 || (RacerArcadeStageGraph.CollisionMask & ent.Comp1.AllLayers) == 0)
            yield break;

        var stage = _prototype.Index(state.CurrentStage);
        if (!stage.Graph.AABB.Intersects(ourAABB))
            yield break;

        foreach (var (aId, aEntry, bEntry) in GetCollidingShapes(ent.Comp1.Shapes, stage.Graph.CollisionShapes))
        {
            var box = bEntry.Shape.GetBox();
            var normal = Vector3.Transform(Vector3.UnitZ, box.Quaternion);
            var contactHeight = ent.Comp2.Position.Z - Vector3.Dot(ent.Comp2.Position - box.Origin, normal);

            var ev = new RacerArcadeObjectCollisionWithTrackEvent(aId, contactHeight, normal);
            yield return (ent.Owner, ev);
        }
    }

    private void UpdateCachedAABB(Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent> ent)
    {
        var box = Box3.Empty;
        foreach (var entry in ent.Comp1.Shapes.Values)
        {
            var shapeBox = entry.Shape.GetBox();
            var shapeAABB = shapeBox.CalcBoundingBox();

            box = new Box3(
                Vector3.ComponentMin(box.LeftBottomBack, shapeAABB.LeftBottomBack),
                Vector3.ComponentMax(box.RightTopFront, shapeAABB.RightTopFront)
            );
        }
        ent.Comp1.CachedAABB = box;
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

    private static IEnumerable<(string aId, RacerArcadeCollisionShapeEntry aEntry, string bId, RacerArcadeCollisionShapeEntry bEntry)> GetCollidingShapes(
        Dictionary<string, RacerArcadeCollisionShapeEntry> a,
        Dictionary<string, RacerArcadeCollisionShapeEntry> b)
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

                yield return (aId, aEntry, bId, bEntry);
            }
        }
    }

    private static IEnumerable<(string aId, RacerArcadeCollisionShapeEntry aEntry, RacerArcadeCollisionShapeEntry bEntry)> GetCollidingShapes(
        Dictionary<string, RacerArcadeCollisionShapeEntry> a,
        List<RacerArcadeCollisionShapeEntry> b)
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

                yield return (aId, aEntry, bEntry);
            }
        }
    }

    [PublicAPI]
    public Box3 GetAABB(Entity<RacerArcadeObjectCollisionComponent?, RacerArcadeObjectComponent?> ent)
    {
        if (!_collision.Resolve(ent.Owner, ref ent.Comp1) || !_data.Resolve(ent.Owner, ref ent.Comp2))
            return Box3.Empty;

        UpdateCachedAABB(ent!);
        var box = new Box3Rotated(ent.Comp1.CachedAABB);
        box = box.Translate(ent.Comp2.Position);
        box = box.Rotate(ent.Comp2.Rotation);
        return box.CalcBoundingBox();
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
