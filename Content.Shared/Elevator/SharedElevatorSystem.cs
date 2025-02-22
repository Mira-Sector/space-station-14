using Content.Shared.DeviceLinking;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Shared.Elevator;

public abstract partial class SharedElevatorSystem : EntitySystem
{
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] protected readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElevatorEntranceComponent, ComponentInit>(OnEntranceInit);
        SubscribeLocalEvent<ElevatorEntranceComponent, ElevatorAttemptTeleportEvent>(OnAttemptTeleport);

        SubscribeLocalEvent<ElevatorExitComponent, ComponentInit>(OnExitInit);
        SubscribeLocalEvent<ElevatorExitComponent, ElevatorTeleportEvent>(OnTeleport);

        SubscribeLocalEvent<ElevatorCollisionComponent, ComponentInit>(OnCollisionInit);
        SubscribeLocalEvent<ElevatorCollisionComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<ElevatorCollisionComponent, StepTriggeredOnEvent>(OnStartCollide);
        SubscribeLocalEvent<ElevatorCollisionComponent, StepTriggeredOffEvent>(OnEndCollide);
        SubscribeLocalEvent<ElevatorCollisionComponent, ElevatorGetEntityOffsetsEvent>(OnCollisionGetOffsets);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var entranceQuery = EntityQueryEnumerator<ElevatorEntranceComponent>();

        while (entranceQuery.MoveNext(out var entranceUid, out var entranceComp))
        {
            if (entranceComp.NextTeleport is not {} nextTeleport)
                continue;

            if (nextTeleport > _timing.CurTime)
                continue;

            if (entranceComp.NextTeleportEntities == null)
                continue;

            if (entranceComp.StartingMap is not {} entranceMap)
                continue;

            if (entranceComp.Exit is not {} exitUid)
                continue;

            if (!TryComp<ElevatorExitComponent>(exitUid, out var exitComp))
                continue;

            if (exitComp.StartingMap is not {} exitMap)
                continue;

            _deviceLink.InvokePort(entranceUid, entranceComp.FinishedPort);

            var ev = new ElevatorTeleportEvent(entranceComp.NextTeleportEntities, entranceMap, exitMap);
            RaiseLocalEvent(exitUid, ev);
            entranceComp.NextTeleport = null;
            entranceComp.NextTeleportEntities = null;
        }
    }

    private void OnEntranceInit(EntityUid uid, ElevatorEntranceComponent component, ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(uid, component.FinishedPort, component.DelayPort);
    }

    private void OnExitInit(EntityUid uid, ElevatorExitComponent component, ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(uid, component.FinishedPort, component.DelayPort);
    }

    private void OnCollisionInit(EntityUid uid, ElevatorCollisionComponent component, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, component.InputPort);
    }

    private void OnStepTriggerAttempt(EntityUid uid, ElevatorCollisionComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue = !component.Collided.Contains(GetNetEntity(args.Tripper));
    }

    private void OnStartCollide(EntityUid uid, ElevatorCollisionComponent component, ref StepTriggeredOnEvent args)
    {
        // purposfully dont store the offset
        // they are likely to move about whilst still being in the collision
        component.Collided.Add(GetNetEntity(args.Tripper));
    }

    private void OnEndCollide(EntityUid uid, ElevatorCollisionComponent component, ref StepTriggeredOffEvent args)
    {
        component.Collided.Remove(GetNetEntity(args.Tripper));
    }

    protected void Teleport(EntityUid uid, ElevatorEntranceComponent component, HashSet<NetEntity> entities)
    {
        if (component.Exit is not {} exitUid)
            return;

        if (component.StartingMap is not {} entranceMap)
            return;

        if (!TryComp<ElevatorExitComponent>(exitUid, out var exitComp))
            return;

        if (exitComp.StartingMap is not {} exitMap)
            return;

        var ev = new ElevatorAttemptTeleportEvent(entities, entranceMap, exitMap);
        RaiseLocalEvent(exitUid, ev);
    }

    private void OnAttemptTeleport(EntityUid uid, ElevatorEntranceComponent component, ElevatorAttemptTeleportEvent args)
    {
        // ongoing delay
        if (component.NextTeleportEntities != null)
            return;

        if (component.Exit is not {} exit)
            return;


        if (component.Delay == null)
        {
            _deviceLink.InvokePort(uid, component.FinishedPort);

            var ev = new ElevatorTeleportEvent(args);
            RaiseLocalEvent(exit, ev);
            return;
        }

        _deviceLink.InvokePort(uid, component.DelayPort);

        if (TryComp<ElevatorExitComponent>(exit, out var exitComp))
            _deviceLink.InvokePort(exit, component.DelayPort);

        component.NextTeleport = _timing.CurTime + component.Delay;
        component.NextTeleportEntities = args.Entities;
    }

    private void OnTeleport(EntityUid uid, ElevatorExitComponent component, ElevatorTeleportEvent args)
    {
        var xform = Transform(uid);

        var targetMap = _map.GetMap(args.TargetMap);
        var originPos = xform.Coordinates.Position;

        var offsetEv = new ElevatorGetEntityOffsetsEvent(args);
        RaiseLocalEvent(uid, offsetEv);

        var ev = new ElevatorGotTeleportedEvent(args.SourceMap, args.TargetMap);

        foreach (var (netEnt, offset) in offsetEv.Offsets)
        {
            var entity = GetEntity(netEnt);
            var coords = new EntityCoordinates(targetMap, Vector2.Add(originPos, xform.LocalRotation.RotateVec(offset)));

            _xform.SetCoordinates(entity, coords);

            RaiseLocalEvent(entity, ev);
        }

        _deviceLink.InvokePort(uid, component.FinishedPort);
    }

    private void OnCollisionGetOffsets(EntityUid uid, ElevatorCollisionComponent component, ElevatorGetEntityOffsetsEvent args)
    {
        var xform = Transform(uid);
        var coords = xform.Coordinates.Position;

        foreach (var entity in args.Entities)
        {
            var entCoords = Transform(GetEntity(entity)).Coordinates.Position;
            args.Offsets.Add(entity, xform.LocalRotation.RotateVec(Vector2.Subtract(coords, entCoords)));
        }
    }
}
