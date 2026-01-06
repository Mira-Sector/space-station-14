using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
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

            if (!TryGetExit((entranceUid, entranceComp), out var exit))
                continue;

            if (exit.Value.Comp.StartingMap is not { } exitMap)
                continue;

            _deviceLink.InvokePort(entranceUid, entranceComp.FinishedPort);

            var ev = new ElevatorTeleportEvent(entranceComp.NextTeleportEntities, entranceMap, exitMap);
            RaiseLocalEvent(exit.Value.Owner, ev);
            entranceComp.NextTeleport = null;
            entranceComp.NextTeleportEntities = null;
            Dirty(entranceUid, entranceComp);
        }
    }

    private void OnEntranceInit(Entity<ElevatorEntranceComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(ent.Owner, ent.Comp.FinishedPort, ent.Comp.DelayPort);
    }

    private void OnExitInit(Entity<ElevatorExitComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(ent.Owner, ent.Comp.FinishedPort, ent.Comp.DelayPort);
    }

    private void OnCollisionInit(Entity<ElevatorCollisionComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent.Owner, ent.Comp.InputPort);
    }

    private void OnStepTriggerAttempt(Entity<ElevatorCollisionComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue = !ent.Comp.Collided.Contains(args.Tripper);
    }

    private void OnStartCollide(Entity<ElevatorCollisionComponent> ent, ref StepTriggeredOnEvent args)
    {
        // purposefully dont store the offset
        // they are likely to move about whilst still being in the collision
        ent.Comp.Collided.Add(args.Tripper);
        Dirty(ent);
    }

    private void OnEndCollide(Entity<ElevatorCollisionComponent> ent, ref StepTriggeredOffEvent args)
    {
        ent.Comp.Collided.Remove(args.Tripper);
        Dirty(ent);
    }

    private void OnAttemptTeleport(Entity<ElevatorEntranceComponent> ent, ref ElevatorAttemptTeleportEvent args)
    {
        // ongoing delay
        if (ent.Comp.NextTeleportEntities != null)
            return;

        if (!TryGetExit(ent, out var exit))
            return;

        if (ent.Comp.Delay is not { } delay)
        {
            _deviceLink.InvokePort(ent.Owner, ent.Comp.FinishedPort);

            var exitEv = new ElevatorTeleportEvent(args);
            RaiseLocalEvent(exit.Value, exitEv);
            return;
        }

        var entranceEv = new ElevatorTeleportingEvent(args);
        RaiseLocalEvent(ent.Owner, entranceEv);

        _deviceLink.InvokePort(ent.Owner, ent.Comp.DelayPort);

        _deviceLink.InvokePort(exit.Value, ent.Comp.DelayPort);

        ent.Comp.NextTeleport = _timing.CurTime + delay;
        ent.Comp.NextTeleportEntities = args.Entities;
        Dirty(ent);
    }

    private void OnMapInit(Entity<ElevatorStationComponent> ent, ref MapInitEvent args)
    {
        foreach (var (key, path) in ent.Comp.ElevatorMapPaths)
        {
            _map.CreateMap(out var mapId);
            if (!_mapLoader.TryLoadMapWithId(mapId, path, out var map, out _, MapLoadOptions))
            {
                _map.DeleteMap(mapId);
                continue;
            }

            _metaData.SetEntityName(map.Value, key);
            ent.Comp.ElevatorMaps.Add(key, mapId);
        }

        Dirty(ent);

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
            Dirty(exitUid, exitComp);

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

            if (!ent.Comp.ElevatorMaps.TryGetValue(entranceComp.ElevatorMapKey, out var netMap))
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
            Dirty(entranceUid, entranceComp);
        }
    }


    private void OnTeleport(Entity<ElevatorExitComponent> ent, ref ElevatorTeleportEvent args)
    {
        var xform = Transform(ent.Owner);

        var targetMap = _map.GetMap(args.TargetMap);
        var originPos = xform.Coordinates.Position;

        var offsetEv = new ElevatorGetEntityOffsetsEvent(args);
        RaiseLocalEvent(ent.Owner, offsetEv);

        var ev = new ElevatorGotTeleportedEvent(args.SourceMap, args.TargetMap);

        foreach (var (entity, offset) in offsetEv.Offsets)
        {
            var coords = new EntityCoordinates(targetMap, Vector2.Add(originPos, xform.LocalRotation.RotateVec(offset)));
            _xform.SetCoordinates(entity, coords);
            RaiseLocalEvent(entity, ev);
        }

        _deviceLink.InvokePort(ent.Owner, ent.Comp.FinishedPort);
    }

    private void OnCollisionGetOffsets(Entity<ElevatorCollisionComponent> ent, ref ElevatorGetEntityOffsetsEvent args)
    {
        var xform = Transform(ent.Owner);
        var coords = xform.Coordinates.Position;

        foreach (var entity in args.Entities)
        {
            var entCoords = Transform(entity).Coordinates.Position;
            args.Offsets.Add(entity, xform.LocalRotation.RotateVec(Vector2.Subtract(coords, entCoords)));
        }
    }

    private void OnMusicAttempt(Entity<ElevatorMusicComponent> ent, ref ElevatorTeleportingEvent args)
    {
        if (_audio.PlayPvs(ent.Comp.Music, ent.Owner, new AudioParams().WithLoop(true).WithPlayOffset(ent.Comp.NextPlayOffset)) is not { } sound)
            return;

        ent.Comp.SoundEntity = sound;
    }

    private void OnMusicTeleport(Entity<ElevatorMusicComponent> ent, ref ElevatorTeleportedEvent args)
    {
        if (ent.Comp.SoundEntity is not { } sound)
            return;

        ent.Comp.NextPlayOffset = sound.Comp.PlaybackPosition;
        Dirty(ent);

        _audio.Stop(sound.Owner, sound.Comp);
        ent.Comp.SoundEntity = null;
    }

    private void OnCollisionSignal(Entity<ElevatorCollisionComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.InputPort)
            return;

        Teleport(ent.Owner, ent.Comp.Collided);
        ent.Comp.Collided.Clear();
        Dirty(ent);
    }

    protected void Teleport(Entity<ElevatorEntranceComponent?> ent, HashSet<EntityUid> entities)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.StartingMap is not { } entranceMap)
            return;

        if (!TryGetExit(ent!, out var exit))
            return;

        if (exit.Value.Comp.StartingMap is not { } exitMap)
            return;

        var ev = new ElevatorAttemptTeleportEvent(entities, entranceMap, exitMap);
        RaiseLocalEvent(exit.Value, ev);
    }

    protected bool TryGetExit(Entity<ElevatorEntranceComponent> entrance, [NotNullWhen(true)] out Entity<ElevatorExitComponent>? exit)
    {
        if (!TryComp<ElevatorExitComponent>(entrance.Comp.Exit, out var exitComp))
        {
            exit = null;
            return false;
        }

        exit = (entrance.Comp.Exit.Value, exitComp);
        return true;
    }

}
