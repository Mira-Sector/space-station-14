using Content.Shared.Holodeck;
using Content.Shared.Holodeck.Components;
using Content.Shared.Holodeck.Ui;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Holodeck;

public sealed partial class HolodeckSystem : SharedHolodeckSystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolodeckSpawnerComponent, HolodeckSpawnerScenarioPickedMessage>(OnSpawnerScenarioPicked);
    }

    private void OnSpawnerScenarioPicked(Entity<HolodeckSpawnerComponent> ent, ref HolodeckSpawnerScenarioPickedMessage args)
    {
        SelectScenario(ent, args.Scenario);
    }

    private void SelectScenario(Entity<HolodeckSpawnerComponent> ent, ProtoId<HolodeckScenarioPrototype>? scenarioId)
    {
        HolodeckScenarioPrototype? scenario;
        if (scenarioId != null)
        {
            scenario = Prototypes.Index(scenarioId);
            if (!IsScenarioSpawnable(ent!, scenario, out _))
                return;
        }
        else
        {
            scenario = null;
        }

        var dirty = false;

        NetEntity map;
        MapId mapId;
        if (ent.Comp.SpawnedScenarioData is { } existingData)
        {
            Del(GetEntity(existingData.Grid));
            ent.Comp.SpawnedScenarioData = null;
            dirty = true;

            map = existingData.Map;
            mapId = existingData.MapId;
        }
        else
        {
            map = GetNetEntity(_map.CreateMap(out mapId, false));
        }

        // nothing else to do
        if (scenario == null)
        {
            if (dirty)
                Dirty(ent);

            return;
        }

        if (!_mapLoader.TryLoadGrid(mapId, scenario.Grid, out var grid))
            throw new Exception($"Tried loading {scenario.ID} grid but failed!");

        var newData = new HolodeckSpawnedScenarioData()
        {
            Grid = GetNetEntity(grid.Value),
            Map = map,
            MapId = mapId,
            Prototype = scenarioId!.Value,
        };

        ent.Comp.SpawnedScenarioData = newData;
        Dirty(ent);
    }
}
