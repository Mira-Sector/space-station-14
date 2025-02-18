using Content.Server.Station.Systems;
using Content.Shared.Elevator;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Conent.Server.Elevator;

public sealed partial class ElevatorSystem : SharedElevatorSystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ElevatorStationComponent, MapInitEvent>(OnMapInit);
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

            component.ElevatorMaps.Add(key, GetNetEntity(mapUid.Value));
        }

        var entranceQuery = EntityQueryEnumerator<ElevatorEntranceComponent>();
        var exitQuery = EntityQueryEnumerator<ElevatorExitComponent>();

        // construct a dictionary for faster lookups
        Dictionary<EntityUid, Dictionary<string, EntityUid>> mapToExitId = new();
        while (exitQuery.MoveNext(out var exitUid, out var exitComp))
        {
            var station = _station.GetOwningStation(exitUid);

            if (station == null)
                continue;

            exitComp.StartingMap = station;

            if (mapToExitId.TryGetValue(station.Value, out var exitIds))
            {
                exitIds.Add(exitComp.ExitId, exitUid);
            }
            else
            {
                Dictionary<string, EntityUid> newExitIds = new();
                newExitIds.Add(exitComp.ExitId, exitUid);
                mapToExitId.Add(station.Value, newExitIds);
            }
        }

        while (entranceQuery.MoveNext(out var entranceUid, out var entranceComp))
        {
            var station = _station.GetOwningStation(entranceUid);

            if (station != uid)
                continue;

            if (!component.ElevatorMaps.TryGetValue(entranceComp.ElevatorMapKey, out var netMap))
            {
                Log.Error($"Failed to load elevator key {entranceComp.ElevatorMapKey} on {ToPrettyString(entranceUid)}.");
                continue;
            }

            var map = GetEntity(netMap);

            if (!mapToExitId.TryGetValue(map, out var exitIds))
            {
                Log.Error($"Cannot find map {ToPrettyString(map)} in mapToExitId.");
                continue;
            }

            if (!exitIds.TryGetValue(entranceComp.ExitId, out var exit))
            {
                Log.Error($"Cannot find {entranceComp.ExitId} on map {ToPrettyString(map)}.");
                continue;
            }

            entranceComp.ElevatorMap = map;
            entranceComp.StartingMap = station;
            entranceComp.Exit = exit;
        }
    }
}
