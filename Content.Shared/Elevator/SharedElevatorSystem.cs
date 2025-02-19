using Content.Shared.DeviceLinking;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using System.Numerics;

namespace Content.Shared.Elevator;

public abstract partial class SharedElevatorSystem : EntitySystem
{
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElevatorCollisionComponent, ComponentInit>(OnCollisionInit);
        SubscribeLocalEvent<ElevatorCollisionComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ElevatorCollisionComponent, EndCollideEvent>(OnEndCollide);

        SubscribeLocalEvent<ElevatorExitComponent, ElevatorTeleportEvent>(OnTeleport);
    }

    private void OnCollisionInit(EntityUid uid, ElevatorCollisionComponent component, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, component.InputPort);
    }

    private void OnStartCollide(EntityUid uid, ElevatorCollisionComponent component, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != component.CollisionId)
            return;

        // purposfully dont store the offset
        // they are likely to move about whilst still being in the collision
        component.Collided.Add(GetNetEntity(args.OtherEntity));
    }

    private void OnEndCollide(EntityUid uid, ElevatorCollisionComponent component, ref EndCollideEvent args)
    {
        if (args.OurFixtureId != component.CollisionId)
            return;

        component.Collided.Remove(GetNetEntity(args.OtherEntity));
    }

    protected void Teleport(EntityUid uid, ElevatorEntranceComponent component, Dictionary<NetEntity, Vector2> entities)
    {
        if (component.Exit is not {} exitUid)
            return;

        if (component.StartingMap is not {} entranceMap)
            return;

        if (!TryComp<ElevatorExitComponent>(exitUid, out var exitComp))
            return;

        if (exitComp.StartingMap is not {} exitMap)
            return;

        var ev = new ElevatorTeleportEvent(entities, GetNetEntity(entranceMap), GetNetEntity(exitMap));
        RaiseLocalEvent(exitUid, ev);
    }

    private void OnTeleport(EntityUid uid, ElevatorExitComponent component, ElevatorTeleportEvent args)
    {
        var targetMap = GetEntity(args.TargetMap);
        var originPos = Transform(uid).Coordinates.Position;

        foreach (var (netEnt, offset) in args.Entities)
        {
            var entity = GetEntity(netEnt);
            var coords = new EntityCoordinates(targetMap, Vector2.Add(originPos, offset));

            _xform.SetCoordinates(entity, coords);

            var ev = new ElevatorGotTeleportedEvent(args.SourceMap, args.TargetMap);
            RaiseLocalEvent(entity, ev);
        }
    }

    protected void CollisionTeleport(EntityUid uid, ElevatorCollisionComponent component)
    {
        if (!TryComp<ElevatorEntranceComponent>(uid, out var entrance))
            return;

        if (component.Collided.Count <= 0)
            return;

        var coords = Transform(uid).Coordinates.Position;

        Dictionary<NetEntity, Vector2> entities = new();
        foreach (var entity in component.Collided)
        {
            var entCoords = Transform(GetEntity(entity)).Coordinates.Position;

            entities.Add(entity, Vector2.Subtract(coords, entCoords));
        }

        Teleport(uid, entrance, entities);
    }
}
