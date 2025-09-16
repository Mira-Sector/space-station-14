using Content.Shared.Holodeck;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
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
    [Dependency] private readonly EyeSystem _eye = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

    private FrozenDictionary<ProtoId<HolodeckScenarioPrototype>, Entity<MapGridComponent>>? _scenarioGrids;
    private FrozenDictionary<ProtoId<HolodeckScenarioPrototype>, (Entity<EyeComponent>, Vector2i)>? _scenarioEyes;
    private (EntityUid, MapId)? _scenarioGridMap;

    // dont @ me
    private const float AdditionalGridOffset = 16f;

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
        Dictionary<ProtoId<HolodeckScenarioPrototype>, (Entity<EyeComponent>, Vector2i)> scenarioEyes = new(scenarios.Count);

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

            gridOffset.X += bb.Width + AdditionalGridOffset;

            scenarioGrids[scenarioId] = grid.Value;

            var eyeCoords = new EntityCoordinates(grid.Value, grid.Value.Comp.LocalAABB.Center);
            var eyeZoomFloat = grid.Value.Comp.LocalAABB.Size * EyeManager.PixelsPerMeter;
            var eyeZoom = new Vector2i((int)eyeZoomFloat.X, (int)eyeZoomFloat.Y);

            var eye = SpawnAtPosition(null, eyeCoords);
            var eyeComp = AddComp<EyeComponent>(eye);
            _eye.SetDrawFov(eye, false, eyeComp);
            _eye.SetDrawLight((eye, eyeComp), false);

            scenarioEyes[scenarioId] = ((eye, eyeComp), eyeZoom);
        }

        _scenarioGrids = scenarioGrids.ToFrozenDictionary();
        _scenarioEyes = scenarioEyes.ToFrozenDictionary();
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

    public bool TryGetScenarioEye(ProtoId<HolodeckScenarioPrototype> scenario, [NotNullWhen(true)] out (Entity<EyeComponent>, Vector2i)? eye)
    {
        if (_scenarioEyes == null)
            InitializeGrids();

        if (_scenarioEyes!.TryGetValue(scenario, out var scenarioEye))
        {
            eye = scenarioEye;
            return true;
        }
        else
        {
            eye = null;
            return false;
        }
    }
}
