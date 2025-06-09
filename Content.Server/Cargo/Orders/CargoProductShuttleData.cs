using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Orders;
using Content.Shared.Cargo.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Cargo.Orders;

public sealed partial class CargoProductShuttleData : SharedCargoProductShuttleData, IServerCargoProductData
{
    public EntityUid? FulfillOrder(Entity<StationDataComponent> stationData, ProtoId<CargoAccountPrototype> account, CargoOrderData order, StationCargoOrderDatabaseComponent orderDatabase)
    {
        var entity = IoCManager.Resolve<IEntityManager>();
        var random = IoCManager.Resolve<IRobustRandom>();

        var cargo = entity.System<CargoSystem>();
        var map = entity.System<MapSystem>();
        var mapLoader = entity.System<MapLoaderSystem>();
        var shuttle = entity.System<ShuttleSystem>();
        var transform = entity.System<TransformSystem>();

        map.CreateMap(out var pausedMap);
        if (!mapLoader.TryLoadGrid(pausedMap, Shuttle, out var shuttleGrid))
        {
            map.DeleteMap(pausedMap);
            return null;
        }

        entity.EnsureComponent<CargoOrderedShuttleComponent>(shuttleGrid.Value).SourceMap = pausedMap;
        entity.EnsureComponent<ShuttleComponent>(shuttleGrid.Value, out var shuttleComp);

        foreach (var trade in cargo.GetTradeStations(stationData))
        {
            shuttle.FTLToDock(shuttleGrid.Value, shuttleComp, trade, 0f);

            // spawn the paper on a random pallet
            var tradePads = cargo.GetCargoPallets(trade, BuySellType.Buy);
            random.Shuffle(tradePads);

            var freePads = cargo.GetFreeCargoPallets(trade, tradePads);
            foreach (var (_, _, palletXform) in freePads)
            {
                var paper = cargo.SpawnPaper(order, account, orderDatabase.PrinterOutput);
                transform.SetCoordinates(paper, palletXform.Coordinates);
                break;
            }

            return trade;
        }

        map.DeleteMap(pausedMap);
        return null;
    }
}
