using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Shared.Elevator;

public abstract partial class SharedElevatorSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    private static readonly DeserializationOptions MapLoadOptions = new()
    {
        InitializeMaps = true
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElevatorEntranceComponent, ComponentInit>(OnEntranceInit);
        SubscribeLocalEvent<ElevatorEntranceComponent, ElevatorAttemptTeleportEvent>(OnAttemptTeleport);

        SubscribeLocalEvent<ElevatorStationComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<ElevatorExitComponent, ComponentInit>(OnExitInit);
        SubscribeLocalEvent<ElevatorExitComponent, ElevatorTeleportEvent>(OnTeleport);

        SubscribeLocalEvent<ElevatorCollisionComponent, ComponentInit>(OnCollisionInit);
        SubscribeLocalEvent<ElevatorCollisionComponent, SignalReceivedEvent>(OnCollisionSignal);
        SubscribeLocalEvent<ElevatorCollisionComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<ElevatorCollisionComponent, StepTriggeredOnEvent>(OnStartCollide);
        SubscribeLocalEvent<ElevatorCollisionComponent, StepTriggeredOffEvent>(OnEndCollide);
        SubscribeLocalEvent<ElevatorCollisionComponent, ElevatorGetEntityOffsetsEvent>(OnCollisionGetOffsets);

        SubscribeLocalEvent<ElevatorMusicComponent, ElevatorTeleportingEvent>(OnMusicAttempt);
        SubscribeLocalEvent<ElevatorMusicComponent, ElevatorTeleportedEvent>(OnMusicTeleport);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var entranceQuery = EntityQueryEnumerator<ElevatorEntranceComponent>();

        while (entranceQuery.MoveNext(out var entranceUid, out var entranceComp))
        {
            if (entranceComp.NextTeleport is not { } nextTeleport)
                continue;

            if (nextTeleport > _timing.CurTime)
                continue;

            if (entranceComp.NextTeleportEntities == null)
                continue;

            if (entranceComp.StartingMap is not { } entranceMap)
                continue;

            if (entranceComp.Exit is not { } exitUid)
                continue;

            if (!TryComp<ElevatorExitComponent>(exitUid, out var exitComp))
                continue;

            if (exitComp.StartingMap is not { } exitMap)
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
        args.Continue = !component.Collided.Contains(args.Tripper);
    }

    private void OnStartCollide(EntityUid uid, ElevatorCollisionComponent component, ref StepTriggeredOnEvent args)
    {
        // purposefully dont store the offset
        // they are likely to move about whilst still being in the collision
        component.Collided.Add(args.Tripper);
    }

    private void OnEndCollide(EntityUid uid, ElevatorCollisionComponent component, ref StepTriggeredOffEvent args)
    {
        component.Collided.Remove(args.Tripper);
    }

    protected void Teleport(EntityUid uid, ElevatorEntranceComponent component, HashSet<EntityUid> entities)
    {
        if (component.Exit is not { } exitUid)
            return;

        if (component.StartingMap is not { } entranceMap)
            return;

        if (!TryComp<ElevatorExitComponent>(exitUid, out var exitComp))
            return;

        if (exitComp.StartingMap is not { } exitMap)
            return;

        var ev = new ElevatorAttemptTeleportEvent(GetNetEntitySet(entities), entranceMap, exitMap);
        RaiseLocalEvent(exitUid, ev);
    }

    private void OnAttemptTeleport(EntityUid uid, ElevatorEntranceComponent component, ElevatorAttemptTeleportEvent args)
    {
        // ongoing delay
        if (component.NextTeleportEntities != null)
            return;

        if (component.Exit is not { } exit)
            return;

        if (component.Delay == null)
        {
            _deviceLink.InvokePort(uid, component.FinishedPort);

            var exitEv = new ElevatorTeleportEvent(args);
            RaiseLocalEvent(exit, exitEv);
            return;
        }

        var entranceEv = new ElevatorTeleportingEvent(args);
        RaiseLocalEvent(uid, entranceEv);

        _deviceLink.InvokePort(uid, component.DelayPort);

        if (TryComp<ElevatorExitComponent>(exit, out var exitComp))
            _deviceLink.InvokePort(exit, component.DelayPort);

        component.NextTeleport = _timing.CurTime + component.Delay;
        component.NextTeleportEntities = args.Entities;
        Dirty(uid, component);
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

    private void OnMusicAttempt(EntityUid uid, ElevatorMusicComponent component, ElevatorTeleportingEvent args)
    {
        var sound = _audio.PlayPvs(component.Music, uid, new AudioParams().WithLoop(true).WithPlayOffset(component.NextPlayOffset));

        if (sound == null)
            return;

        component.SoundEntity = sound.Value;
    }

    private void OnMusicTeleport(EntityUid uid, ElevatorMusicComponent component, ElevatorTeleportedEvent args)
    {
        if (component.SoundEntity == null)
            return;

        var (soundUid, soundComp) = component.SoundEntity.Value;

        component.NextPlayOffset = soundComp.PlaybackPosition;

        _audio.Stop(soundUid);
        component.SoundEntity = null;
    }

    private void OnCollisionSignal(EntityUid uid, ElevatorCollisionComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port != component.InputPort)
            return;

        if (!TryComp<ElevatorEntranceComponent>(uid, out var entrance))
            return;

        Teleport(uid, entrance, component.Collided);
        component.Collided.Clear();
    }

    private void OnMapInit(EntityUid uid, ElevatorStationComponent component, MapInitEvent args)
    {
        foreach (var (key, path) in component.ElevatorMapPaths)
        {
            _map.CreateMap(out var mapId);
            if (!_mapLoader.TryLoadMapWithId(mapId, path, out var map, out _, MapLoadOptions))
            {
                _map.DeleteMap(mapId);
                continue;
            }

            _metaData.SetEntityName(map.Value, key);
            component.ElevatorMaps.Add(key, mapId);
        }

        var entranceQuery = EntityQueryEnumerator<ElevatorEntranceComponent>();
        var exitQuery = EntityQueryEnumerator<ElevatorExitComponent>();

        // construct a dictionary for faster lookups
        Dictionary<MapId, Dictionary<string, EntityUid>> mapToExitId = [];
        while (exitQuery.MoveNext(out var exitUid, out var exitComp))
        {
            var map = Transform(exitUid).MapID;

            if (map == MapId.Nullspace)
                continue;

            exitComp.StartingMap = map;

            if (mapToExitId.TryGetValue(map, out var exitIds))
            {
                exitIds.Add(exitComp.ExitId, exitUid);
            }
            else
            {
                Dictionary<string, EntityUid> newExitIds = new();
                newExitIds.Add(exitComp.ExitId, exitUid);
                mapToExitId.Add(map, newExitIds);
            }
        }

        while (entranceQuery.MoveNext(out var entranceUid, out var entranceComp))
        {
            var map = Transform(entranceUid).MapID;

            if (map == MapId.Nullspace)
                continue;

            if (!component.ElevatorMaps.TryGetValue(entranceComp.ElevatorMapKey, out var netMap))
            {
                Log.Error($"Failed to load elevator key {entranceComp.ElevatorMapKey} on {ToPrettyString(entranceUid)}.");
                continue;
            }

            if (!mapToExitId.TryGetValue(netMap, out var exitIds))
            {
                Log.Error($"Cannot find map {map} in mapToExitId.");
                continue;
            }

            if (!exitIds.TryGetValue(entranceComp.ExitId, out var exit))
            {
                Log.Error($"Cannot find {entranceComp.ExitId} on map {map}.");
                continue;
            }

            entranceComp.ElevatorMap = netMap;
            entranceComp.StartingMap = map;
            entranceComp.Exit = exit;
        }
    }
}
