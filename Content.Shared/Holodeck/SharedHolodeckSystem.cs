using Content.Shared.Holodeck.Components;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Holodeck;

public abstract partial class SharedHolodeckSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitions = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolodeckSpawnerComponent, ComponentInit>(OnSpawnerInit);
        SubscribeLocalEvent<HolodeckSpawnerComponent, ComponentRemove>(OnSpawnerRemove);
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

        var centerVec = ent.Comp.Center.ToVector2i(EntityManager, _mapMan, _xform);

        if (scenario.RequiredSpace is { } requiredSpace)
        {
            // no grid so no available tiles
            if (_xform.GetGrid(ent.Comp.Center) is not { } grid)
                return false;

            if (!TryComp<MapGridComponent>(grid, out var gridComp))
                return false;

            if (!CheckScenarioSpace(requiredSpace, centerVec, (grid, gridComp), ref adjustedBounds))
                return false;
        }

        return true;
    }

    private bool CheckScenarioSpace(List<Box2i> requiredSpace, Vector2i centerVec, Entity<MapGridComponent> grid, ref List<Box2i>? adjustedBounds)
    {
        adjustedBounds = new(requiredSpace.Count);

        foreach (var box in requiredSpace)
        {
            var boxRelative = box.Translated(centerVec);
            adjustedBounds.Add(boxRelative);

            var expectedTileCount = box.Width * box.Height;
            var tiles = _map.GetLocalTilesIntersecting(grid.Owner, grid.Comp, boxRelative);

            if (tiles.Count() != expectedTileCount)
                return false;

            foreach (var tile in tiles)
            {
                if (!tile.Tile.GetContentTileDefinition(_tileDefinitions).AllowHolodeck)
                    return false;

                //TODO: check nothing like walls is blocked
            }
        }

        return true;
    }
}
