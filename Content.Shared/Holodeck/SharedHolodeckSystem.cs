using Content.Shared.Holodeck.Components;
using Content.Shared.Holodeck.Ui;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Holodeck;

public abstract partial class SharedHolodeckSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] protected readonly IPrototypeManager Prototypes = default!;
    [Dependency] protected readonly SharedTransformSystem Xform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private const LookupFlags FloorLookupFlags = LookupFlags.Approximate | LookupFlags.Static;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolodeckSpawnerComponent, ComponentInit>(OnSpawnerInit);
        SubscribeLocalEvent<HolodeckSpawnerComponent, ComponentRemove>(OnSpawnerRemove);
        SubscribeLocalEvent<HolodeckSpawnerComponent, BoundUIOpenedEvent>(OnSpawnerUiOpen);
    }

    private void OnSpawnerInit(Entity<HolodeckSpawnerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Center = Transform(ent.Owner).Coordinates;
        Dirty(ent);
    }

    private void OnSpawnerRemove(Entity<HolodeckSpawnerComponent> ent, ref ComponentRemove args)
    {
        CleanupSpawned(ent);
    }

    private void OnSpawnerUiOpen(Entity<HolodeckSpawnerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateSpawnerUi(ent);
    }

    protected void UpdateSpawnerUi(Entity<HolodeckSpawnerComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, HolodeckSpawnerUiKey.Key))
            return;

        var center = GetNetCoordinates(ent.Comp.Center);
        var state = new HolodeckSpawnerBoundUserInterfaceState(center, ent.Comp.Scenarios);
        _ui.SetUiState(ent.Owner, HolodeckSpawnerUiKey.Key, state);
    }

    private void CleanupSpawned(Entity<HolodeckSpawnerComponent> ent)
    {
        foreach (var spawned in ent.Comp.Spawned)
            Del(spawned);

        ent.Comp.Spawned.Clear();
        Dirty(ent);
    }

    public bool IsScenarioSpawnable(Entity<HolodeckSpawnerComponent?> ent, HolodeckScenarioPrototype scenario, out List<Box2i>? adjustedBounds)
    {
        adjustedBounds = null;

        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        var centerVec = ent.Comp.Center.ToVector2i(EntityManager, _map, Xform);

        if (scenario.RequiredSpace is { } requiredSpace)
        {
            // no grid so no available tiles
            if (Xform.GetGrid(ent.Comp.Center) is not { } grid)
                return false;

            if (!TryComp<MapGridComponent>(grid, out var gridComp))
                return false;

            if (GetScenarioMissingTiles(requiredSpace, centerVec, (grid, gridComp), ref adjustedBounds).Any())
                return false;
        }

        return true;
    }

    private HashSet<Vector2i> GetScenarioMissingTiles(List<Box2i> requiredSpace, Vector2i centerVec, Entity<MapGridComponent> grid, ref List<Box2i>? adjustedBounds)
    {
        HashSet<Vector2i> found = [];
        HashSet<Vector2i> missing = [];

        adjustedBounds = new(requiredSpace.Count);

        foreach (var box in requiredSpace)
        {
            var boxRelative = box.Translated(centerVec);
            adjustedBounds.Add(boxRelative);

            var expectedTileCount = box.Width * box.Height;
            HashSet<Entity<HolodeckFloorComponent>> floors = new(expectedTileCount);
            _entityLookup.GetLocalEntitiesIntersecting(grid.Owner, boxRelative, floors, FloorLookupFlags);

            found.EnsureCapacity(found.Count + expectedTileCount);
            missing.EnsureCapacity(missing.Count + expectedTileCount);

            foreach (var floor in floors)
            {
                //TODO: check nothing like walls is blocking
                var floorIndices = Xform.GetGridTilePositionOrDefault(floor.Owner, grid.Comp);
                found.Add(floorIndices);
            }

            for (var x = boxRelative.Left; x <= boxRelative.Right; x++)
            {
                for (var y = boxRelative.Bottom; y <= boxRelative.Top; y++)
                {
                    var floorIndices = new Vector2i(x, y);
                    if (!found.Contains(floorIndices))
                        missing.Add(floorIndices);
                }
            }
        }

        return missing;
    }
}
