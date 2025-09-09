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

        SubscribeLocalEvent<HolodeckSpawnerComponent, ComponentRemove>(OnSpawnerRemove);
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

    public bool IsScenarioSpawnable(Entity<HolodeckSpawnerComponent?> ent, HolodeckScenarioPrototype scenario)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        var centerVec = ent.Comp.Center.ToVector2i(EntityManager, _mapMan, _xform);

        if (scenario.RequiredSpace.Any())
        {
            if (!CheckScenarioSpace(ent!, scenario, centerVec))
                return false;
        }

        return true;
    }

    private bool CheckScenarioSpace(Entity<HolodeckSpawnerComponent> ent, HolodeckScenarioPrototype scenario, Vector2i centerVec)
    {
        // no grid so no available tiles
        if (_xform.GetGrid(ent.Comp.Center) is not { } grid)
            return false;

        if (!TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        foreach (var box in scenario.RequiredSpace)
        {
            var boxRelative = box.Translated(centerVec);
            var tiles = _map.GetLocalTilesIntersecting(grid, gridComp, boxRelative);

            // no available tiles, it is space
            if (!tiles.Any())
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
