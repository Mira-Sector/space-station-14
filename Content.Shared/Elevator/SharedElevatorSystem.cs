using Content.Shared.DeviceLinking;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Shared.Elevator;

public abstract partial class SharedElevatorSystem : EntitySystem
{
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
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

    protected void RangeTeleport(EntityUid uid, ElevatorRangeComponent component)
    {
        if (!TryComp<ElevatorEntranceComponent>(uid, out var entrance))
            return;

        var xform = Transform(uid);
        var mapId = xform.MapID;
        var coords = xform.Coordinates.Position;

        var box = new Box2(Vector2.Add(component.Range.BottomLeft, coords), Vector2.Add(component.Range.TopRight, coords));

        Dictionary<NetEntity, Vector2> entities = new();
        foreach (var entity in _lookup.GetEntitiesIntersecting(mapId, box))
        {
            var entCoords = Transform(entity).Coordinates.Position;

            entities.Add(GetNetEntity(entity), Vector2.Subtract(coords, entCoords));
        }

        Teleport(uid, entrance, entities);
    }
}
