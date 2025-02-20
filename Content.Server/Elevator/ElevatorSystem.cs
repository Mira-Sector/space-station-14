using Content.Server.DeviceLinking.Events;
using Content.Shared.Elevator;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Conent.Server.Elevator;

public sealed partial class ElevatorSystem : SharedElevatorSystem
{
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ElevatorCollisionComponent, SignalReceivedEvent>(OnCollisionSignal);
        SubscribeLocalEvent<ElevatorStationComponent, MapInitEvent>(OnMapInit);
    }

    private void OnCollisionSignal(EntityUid uid, ElevatorCollisionComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port != component.InputPort)
            return;

        CollisionTeleport(uid, component);
    }

    private void OnMapInit(EntityUid uid, ElevatorStationComponent component, MapInitEvent args)
    {
        foreach (var (key, map) in component.ElevatorMapPaths)
        {
            _map.CreateMap(out var mapId);
            if (!_mapLoader.TryLoad(mapId, map.ToString(), out var roots) ||
                !_map.TryGetMap(mapId, out var mapUid))
            {
                _mapManager.DeleteMap(mapId);
                continue;
            }

            _metaData.SetEntityName(mapUid.Value, key);

            component.ElevatorMaps.Add(key, GetNetEntity(mapUid.Value));
        }

        var entranceQuery = EntityQueryEnumerator<ElevatorEntranceComponent>();
        var exitQuery = EntityQueryEnumerator<ElevatorExitComponent>();

        // construct a dictionary for faster lookups
        Dictionary<MapId, Dictionary<string, EntityUid>> mapToExitId = new();
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

            if (!mapToExitId.TryGetValue(map, out var exitIds))
            {
                Log.Error($"Cannot find map {map.ToString()} in mapToExitId.");
                continue;
            }

            if (!exitIds.TryGetValue(entranceComp.ExitId, out var exit))
            {
                Log.Error($"Cannot find {entranceComp.ExitId} on map {map.ToString()}.");
                continue;
            }

            entranceComp.ElevatorMap = GetEntity(netMap);
            entranceComp.StartingMap = map;
            entranceComp.Exit = exit;
        }
    }
}
