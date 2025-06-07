using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Orders;

public sealed class CargoProductShuttleData : BaseCargoProductData
{
    [DataField]
    public ResPath Path;

    public override bool IsValid()
    {
        return true;
    }

    public override EntityUid? FulfillOrder(Entity<StationDataComponent> stationData, ProtoId<CargoAccountPrototype> account, CargoOrderData order, StationCargoOrderDatabaseComponent orderDatabase)
    {
        var entity = IoCManager.Resolve<IEntityManager>();
        var random = IoCManager.Resolve<IRobustRandom>();

        var cargo = entity.System<CargoSystem>();
        var map = entity.System<MapSystem>();
        var mapLoader = entity.System<MapLoaderSystem>();
        var shuttle = entity.System<ShuttleSystem>();
        var transform = entity.System<TransformSystem>();

        map.CreateMap(out var pausedMap);
        map.SetPaused(pausedMap, true);

        if (!mapLoader.TryLoadGrid(pausedMap, Path, out var shuttleGrid))
        {
            map.DeleteMap(pausedMap);
            return null;
        }

        var shuttleComp = entity.EnsureComponent<ShuttleComponent>(shuttleGrid.Value);
        foreach (var trade in cargo.GetTradeStations(stationData))
        {
            if (!shuttle.TryFTLDock(shuttleGrid.Value, shuttleComp, trade))
            {
                // fallback to teleporting near
                if (!shuttle.TryFTLProximity(shuttleGrid.Value, trade))
                    continue;
            }

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

            map.DeleteMap(pausedMap);
            return trade;
        }

        entity.DeleteEntity(shuttleGrid.Value);
        map.DeleteMap(pausedMap);
        return null;
    }
}
