using Content.Shared.Holodeck;
using Robust.Client.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Content.Client.Holodeck;

public sealed partial class HolodeckSystem : SharedHolodeckSystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

    private FrozenDictionary<ProtoId<HolodeckScenarioPrototype>, Entity<MapGridComponent>>? _scenarioGrids;
    private (EntityUid, MapId)? _scenarioGridMap;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);
    }

    private void InitializeGridsMap()
    {
        var uid = _map.CreateMap(out var mapId, false);
        _scenarioGridMap = (uid, mapId);
    }

    private void InitializeGrids()
    {
        var scenarios = Prototypes.GetInstances<HolodeckScenarioPrototype>();
        Dictionary<ProtoId<HolodeckScenarioPrototype>, Entity<MapGridComponent>> scenarioGrids = new(scenarios.Count);

        if (_scenarioGridMap == null)
            InitializeGridsMap();

        var (_, mapId) = _scenarioGridMap!.Value;

        var gridOffset = Vector2.Zero;
        foreach (var (scenarioId, scenario) in scenarios)
        {
            if (!_mapLoader.TryLoadGrid(mapId, scenario.Grid, out var grid, offset: gridOffset))
                throw new Exception($"Tried loading {scenario.ID} grid but failed!");

            var (gridPos, gridRot) = Xform.GetWorldPositionRotation(grid.Value.Owner);
            var aabb = grid.Value.Comp.LocalAABB.Translated(gridPos);
            var bb = new Box2Rotated(aabb, gridRot, gridPos).CalcBoundingBox();

            gridOffset += new Vector2(bb.Width, 0f);

            scenarioGrids[scenarioId] = grid.Value;
        }

        _scenarioGrids = scenarioGrids.ToFrozenDictionary();
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<HolodeckScenarioPrototype>())
            return;

        // TODO: this doesnt cover converting existing usages to the new gridUid
        // its only called from admemes and testing so...
        // dont worry about it :3
        if (_scenarioGrids != null)
        {
            foreach (var grid in _scenarioGrids.Values)
                Del(grid);
        }

        InitializeGrids();
    }

    public bool TryGetScenarioGrid(ProtoId<HolodeckScenarioPrototype> scenario, [NotNullWhen(true)] out Entity<MapGridComponent>? grid)
    {
        if (_scenarioGrids == null)
            InitializeGrids();

        if (_scenarioGrids!.TryGetValue(scenario, out var scenarioGrid))
        {
            grid = scenarioGrid;
            return true;
        }
        else
        {
            grid = null;
            return false;
        }
    }
}
