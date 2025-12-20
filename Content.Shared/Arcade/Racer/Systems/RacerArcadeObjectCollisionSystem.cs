using Content.Shared.Arcade.Racer.Components;
using Content.Shared.Arcade.Racer.CollisionShapes;
using Content.Shared.Arcade.Racer.Events;
using Content.Shared.Arcade.Racer.Stage;
using Content.Shared.Maths;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.Arcade.Racer.Systems;

public sealed partial class RacerArcadeObjectCollisionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedRacerArcadeSystem _racer = default!;

    private EntityQuery<RacerArcadeObjectCollisionComponent> _collision;
    private EntityQuery<RacerArcadeObjectComponent> _data;

    private readonly struct CollisionEvents()
    {
        public readonly List<(EntityUid, RacerArcadeObjectStartCollisionWithObjectEvent)> StartObject = [];
        public readonly List<(EntityUid, RacerArcadeObjectEndCollisionWithObjectEvent)> EndObject = [];
        public readonly List<(EntityUid, RacerArcadeObjectStartCollisionWithTrackEvent)> StartTrack = [];
        public readonly List<(EntityUid, RacerArcadeObjectEndCollisionWithTrackEvent)> EndTrack = [];

        public readonly CollisionEvents Merge(params CollisionEvents[] others)
        {
            foreach (var other in others)
            {
                StartObject.AddRange(other.StartObject);
                EndObject.AddRange(other.EndObject);
                StartTrack.AddRange(other.StartTrack);
                EndTrack.AddRange(other.EndTrack);
            }

            return this;
        }
    }

    private readonly struct CollisionCollided()
    {
        private readonly Dictionary<(string aId, string bId), RacerArcadeCollisionContact> _contacts = [];

        public readonly void Add(string aId, RacerArcadeCollisionShapeEntry aEntry, string bId, RacerArcadeCollisionShapeEntry bEntry, Vector3 normal, float penetration)
        {
            var contact = new RacerArcadeCollisionContact(aId, aEntry, bId, bEntry, normal, penetration);
            _contacts[(aId, bId)] = contact;
        }

        public readonly bool Contains(string aId, string bId)
        {
            return _contacts.ContainsKey((aId, bId));
        }

        public IEnumerable<RacerArcadeCollisionContact> GetCollisions()
        {
            foreach (var (_, contact) in _contacts)
                yield return contact;
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(RacerArcadeObjectPhysicsSystem));

        SubscribeLocalEvent<RacerArcadeObjectCollisionComponent, ComponentInit>(OnInit);

        _collision = GetEntityQuery<RacerArcadeObjectCollisionComponent>();
        _data = GetEntityQuery<RacerArcadeObjectComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var events = new CollisionEvents();
        var query = EntityQueryEnumerator<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent>();
        while (query.MoveNext(out var uid, out var collision, out var data))
        {
            var newEvents = HandleCollisions((uid, collision, data));
            events.Merge(newEvents);
        }

        /*
         * raise events after collision detection
         * prevent subscribers messing up further collisions
        */
        foreach (var (uid, ev) in events.StartObject)
        {
            var refEv = ev;
            RaiseLocalEvent(uid, ref refEv);
            Log.Debug("Start Object");
        }

        foreach (var (uid, ev) in events.EndObject)
        {
            var refEv = ev;
            RaiseLocalEvent(uid, ref refEv);
            Log.Debug("End Object");
        }

        foreach (var (uid, ev) in events.StartTrack)
        {
            var refEv = ev;
            RaiseLocalEvent(uid, ref refEv);
            Log.Debug("Start Track");
        }

        foreach (var (uid, ev) in events.EndTrack)
        {
            var refEv = ev;
            RaiseLocalEvent(uid, ref refEv);
            Log.Debug("End Track");
        }
    }

    private void OnInit(Entity<RacerArcadeObjectCollisionComponent> ent, ref ComponentInit args)
    {
        var data = _data.Get(ent.Owner);
        UpdateCachedAABB((ent.Owner, ent.Comp, data));
        UpdateCachedCollisionFlags(ent);
    }

    private CollisionEvents HandleCollisions(Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent> ent)
    {
        var entity = HandleEntityCollisions(ent, out var entityCollided);
        var track = HandleTrackCollisions(ent, out var trackCollided);

        var endEntity = HandleEndObjectCollisions(ent, entityCollided);
        var endTrack = HandleEndTrackCollisions(ent, trackCollided);

        var events = new CollisionEvents();
        return events.Merge(entity, track, endEntity, endTrack);
    }

    private CollisionEvents HandleEntityCollisions(
        Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent> ent,
        out CollisionCollided collided)
    {
        var events = new CollisionEvents();
        collided = new();

        if (!_racer.TryGetArcade((ent.Owner, ent.Comp2), out var ourArcade))
            return events;

        var ourAABB = GetSweptAABB(ent);

        var query = EntityQueryEnumerator<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent>();
        while (query.MoveNext(out var uid, out var physics, out var data))
        {
            Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent> other = new(uid, physics, data);
            if (other.Owner == ent.Owner)
                continue;

            if (!_racer.TryGetArcade((other.Owner, other.Comp2), out var otherArcade))
                continue;

            if (ourArcade != otherArcade)
                continue;

            if ((ent.Comp1.AllMasks & other.Comp1.AllLayers) == 0)
                continue;

            var otherAABB = GetSweptAABB(other);
            if (!ourAABB.Intersects(otherAABB))
                continue;

            var netOther = GetNetEntity(other.Owner);

            GetCollidingShapes(ent.Comp1.Shapes, other.Comp1.Shapes, out var currentCollided);
            foreach (var contact in currentCollided.GetCollisions())
            {
                var netContact = GetNetCollisionContact(contact);
                var (aId, aEntry, bId, bEntry, normal, penetration) = contact;

                if (!aEntry.ObjectShapesCollided.TryGetValue(netOther, out var entries))
                    entries = [];

                if (entries.ContainsKey(bId))
                    continue;
                entries[bId] = netContact;

                var ev = new RacerArcadeObjectStartCollisionWithObjectEvent(other.Owner, aId, aEntry, bId, bEntry, normal, penetration);
                events.StartObject.Add((ent.Owner, ev));
                collided.Add(aId, aEntry, bId, bEntry, normal, penetration);
            }
        }

        return events;
    }

    private CollisionEvents HandleTrackCollisions(Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent> ent, out CollisionCollided collided)
    {
        var events = new CollisionEvents();
        collided = new();

        if (!_racer.TryGetArcade((ent.Owner, ent.Comp2), out var arcade))
            return events;

        var ourAABB = GetSweptAABB(ent);

        if (arcade.Value.Comp.State is not { } state)
            return events;

        if ((ent.Comp1.AllMasks & RacerArcadeStageGraph.CollisionLayer) == 0)
            return events;

        var stage = _prototype.Index(state.CurrentStage);
        if (!stage.Graph.AABB.Intersects(ourAABB))
            return events;

        GetCollidingShapes(ent.Comp1.Shapes, stage.Graph.CollisionShapes, out collided);
        foreach (var contact in collided.GetCollisions())
        {
            var netContact = GetNetCollisionContact(contact);
            var (aId, aEntry, bId, bEntry, normal, penetration) = contact;

            if (aEntry.TrackShapesCollided.ContainsKey(bId))
                continue;
            aEntry.TrackShapesCollided[bId] = netContact;

            var ev = new RacerArcadeObjectStartCollisionWithTrackEvent(aId, aEntry, bEntry, normal, penetration);
            events.StartTrack.Add((ent.Owner, ev));
        }

        return events;
    }

    private CollisionEvents HandleEndObjectCollisions(Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent> ent, CollisionCollided collided)
    {
        var events = new CollisionEvents();

        RemQueue<(RacerArcadeCollisionShapeEntry, RemQueue<(NetEntity, RemQueue<string>)>)> remQueue = new();

        foreach (var (aId, aEntry) in ent.Comp1.Shapes)
        {
            RemQueue<(NetEntity, RemQueue<string>)> shapeRemQueue = new();

            foreach (var (netOther, contacts) in aEntry.ObjectShapesCollided)
            {
                var otherUid = GetEntity(netOther);
                var other = _collision.Get(otherUid);

                RemQueue<string> bIdsRemQueue = new();

                foreach (var (bId, contact) in contacts)
                {
                    var bEntry = other.Comp.Shapes[bId];
                    if (collided.Contains(aId, bId))
                        continue;

                    var ev = new RacerArcadeObjectEndCollisionWithObjectEvent(other.Owner, aId, aEntry, bId, bEntry, contact.Normal, contact.Penetration);
                    events.EndObject.Add((ent.Owner, ev));
                    bIdsRemQueue.Add(bId);
                }

                shapeRemQueue.Add((netOther, bIdsRemQueue));
            }

            remQueue.Add((aEntry, shapeRemQueue));
        }

        foreach (var (aEntry, shapeRemQueue) in remQueue)
        {
            foreach (var (other, bIds) in shapeRemQueue)
            {
                var existing = aEntry.ObjectShapesCollided[other];
                foreach (var bId in bIds)
                    existing.Remove(bId);

                if (existing.Any())
                    aEntry.ObjectShapesCollided[other] = existing;
                else
                    aEntry.ObjectShapesCollided.Remove(other);
            }
        }

        return events;
    }

    private CollisionEvents HandleEndTrackCollisions(Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent> ent, CollisionCollided collided)
    {
        var events = new CollisionEvents();

        if (!_racer.TryGetArcade((ent.Owner, ent.Comp2), out var arcade))
            return events;

        if (arcade.Value.Comp.State is not { } state)
            return events;

        var stage = _prototype.Index(state.CurrentStage);

        RemQueue<(RacerArcadeCollisionShapeEntry, RemQueue<string>)> remQueue = new();

        foreach (var (aId, aEntry) in ent.Comp1.Shapes)
        {
            RemQueue<string> shapeRemQueue = new();

            foreach (var (bId, contact) in aEntry.TrackShapesCollided)
            {
                var bEntry = stage.Graph.CollisionShapes[bId];
                if (collided.Contains(aId, bId))
                    continue;

                var ev = new RacerArcadeObjectEndCollisionWithTrackEvent(aId, aEntry, bEntry, contact.Normal, contact.Penetration);
                events.EndTrack.Add((ent.Owner, ev));
                shapeRemQueue.Add(bId);
            }

            remQueue.Add((aEntry, shapeRemQueue));
        }

        foreach (var (aEntry, shapeRemQueue) in remQueue)
        {
            foreach (var bId in shapeRemQueue)
                aEntry.TrackShapesCollided.Remove(bId);
        }

        return events;
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

    private static void GetCollidingShapes(
        Dictionary<string, RacerArcadeCollisionShapeEntry> a,
        Dictionary<string, RacerArcadeCollisionShapeEntry> b,
        out CollisionCollided collided)
    {
        collided = new();

        foreach (var (aId, aEntry) in a)
        {
            var aBox = aEntry.Shape.GetBox().CalcBoundingBox();

            foreach (var (bId, bEntry) in b)
            {
                if ((aEntry.Mask & bEntry.Layer) == 0)
                    continue;

                var bBox = bEntry.Shape.GetBox().CalcBoundingBox();

                if (!aBox.Intersects(bBox))
                    continue;

                if (!RacerArcadeObjectCollisionResolver.Resolve(aEntry.Shape, bEntry.Shape, out var normal, out var penetration))
                    continue;

                collided.Add(aId, aEntry, bId, bEntry, normal.Value, penetration.Value);
            }
        }
    }

    private Box3 GetAABBAtPosition(Entity<RacerArcadeObjectCollisionComponent> ent, Vector3 position, Quaternion rotation)
    {
        UpdateCachedAABB(ent!);
        var box = new Box3Rotated(ent.Comp.CachedAABB);
        box = box.Translate(position);
        box = box.Rotate(rotation);
        return box.CalcBoundingBox();
    }

    private Box3 GetSweptAABB(Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent> ent)
    {
        var startAABB = GetAABBAtPosition((ent.Owner, ent.Comp1), ent.Comp2.PreviousPosition, ent.Comp2.PreviousRotation);
        var endAABB = GetAABBAtPosition((ent.Owner, ent.Comp1), ent.Comp2.Position, ent.Comp2.Rotation);

        return new Box3(
            Vector3.ComponentMin(startAABB.LeftBottomBack, endAABB.LeftBottomBack),
            Vector3.ComponentMax(startAABB.RightTopFront, endAABB.RightTopFront)
        );
    }

    [PublicAPI]
    public NetRacerArcadeCollisionContact GetNetCollisionContact(RacerArcadeCollisionContact contact)
    {
        return new(contact.AId, contact.BId, contact.Normal, contact.Penetration);
    }

    [PublicAPI]
    public RacerArcadeCollisionContact GetCollisionContact(Entity<RacerArcadeObjectCollisionComponent, RacerArcadeObjectComponent?> ent, NetRacerArcadeCollisionContact netContact)
    {
        if (!_racer.TryGetArcade((ent.Owner, ent.Comp2), out var arcade) || arcade.Value.Comp.State is not { } state)
            throw new InvalidOperationException($"Tried to get contacts for {ToPrettyString(ent.Owner)} when the arcade doesnt exist.");

        var stage = _prototype.Index(state.CurrentStage);

        foreach (var (ourId, ourEntry) in ent.Comp1.Shapes)
        {
            if (ourId != netContact.AId)
                continue;

            foreach (var (trackId, trackEntry) in stage.Graph.CollisionShapes)
            {
                if (trackId != netContact.BId)
                    continue;

                return new(ourId, ourEntry, trackId, trackEntry, netContact.Normal, netContact.Penetration);
            }

            foreach (var netOther in ourEntry.ObjectShapesCollided.Keys)
            {
                var otherUid = GetEntity(netOther);
                var other = _collision.Get(otherUid);

                foreach (var (otherId, otherEntry) in other.Comp.Shapes)
                {
                    if (otherId != netContact.BId)
                        continue;

                    return new(ourId, ourEntry, otherId, otherEntry, netContact.Normal, netContact.Penetration);
                }
            }
        }

        throw new InvalidOperationException($"{ToPrettyString(ent.Owner)} does not have the valid contact: {netContact}");
    }

    [PublicAPI]
    public Box3 GetAABB(Entity<RacerArcadeObjectCollisionComponent?, RacerArcadeObjectComponent?> ent)
    {
        if (!_collision.Resolve(ent.Owner, ref ent.Comp1) || !_data.Resolve(ent.Owner, ref ent.Comp2))
            return Box3.Empty;

        return GetAABBAtPosition((ent.Owner, ent.Comp1), ent.Comp2.Position, ent.Comp2.Rotation);
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

        foreach (var (_, entry) in stage.Graph.CollisionShapes)
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
