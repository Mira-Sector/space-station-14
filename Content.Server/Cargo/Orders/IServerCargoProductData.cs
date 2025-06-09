using Content.Server.Cargo.Components;
using Content.Server.Station.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Cargo.Orders;

public interface IServerCargoProductData
{
    EntityUid? FulfillOrder(Entity<StationDataComponent> stationData, ProtoId<CargoAccountPrototype> account, CargoOrderData order, StationCargoOrderDatabaseComponent orderDatabase);
}
