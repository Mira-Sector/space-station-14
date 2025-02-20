using Content.Shared.DeviceLinking;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Shared.Elevator;

public abstract partial class SharedElevatorSystem : EntitySystem
{
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] protected readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElevatorRangeComponent, ComponentInit>(OnRangeInit);

        SubscribeLocalEvent<ElevatorExitComponent, ElevatorTeleportEvent>(OnTeleport);
    }

    private void OnRangeInit(EntityUid uid, ElevatorRangeComponent component, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, component.InputPort);
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

        var ev = new ElevatorTeleportEvent(entities, entranceMap, exitMap);
        RaiseLocalEvent(exitUid, ev);
    }

    private void OnTeleport(EntityUid uid, ElevatorExitComponent component, ElevatorTeleportEvent args)
    {
        var targetMap = _map.GetMap(args.TargetMap);
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

    protected void RangeTeleport(EntityUid uid, ElevatorRangeComponent component)
    {
        if (!TryComp<ElevatorEntranceComponent>(uid, out var entrance))
            return;

        var xform = Transform(uid);
        var mapId = xform.MapID;
        var coords = xform.Coordinates.Position;

        var minX = coords.X + component.Offset.X - component.Range;
        var maxX = coords.X + component.Offset.X + component.Range;
        var minY = coords.Y + component.Offset.Y - component.Range;
        var maxY = coords.Y + component.Offset.Y + component.Range;

        var range = Math.Max(Math.Abs(component.Offset.X), Math.Abs(component.Offset.Y)) + component.Range;

        Dictionary<NetEntity, Vector2> entities = new();
        foreach (var entity in _lookup.GetEntitiesInRange(uid, range, LookupFlags.Dynamic))
        {
            var entCoords = Transform(entity).Coordinates.Position;

            if (entCoords.X < minX || entCoords.X > maxX)
                continue;

            if (entCoords.Y < minY || entCoords.Y > maxY)
                continue;

            var relativeCoords = Vector2.Subtract(coords, entCoords);

            entities.Add(GetNetEntity(entity), relativeCoords);
        }

        Teleport(uid, entrance, entities);
    }
}
