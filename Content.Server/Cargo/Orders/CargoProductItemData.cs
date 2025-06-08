using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Labels.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Cargo.Orders;

public sealed partial class CargoProductItemData : BaseCargoProductData
{
    /// <summary>
    ///     The entity prototype ID of the product.
    /// </summary>
    [DataField]
    public EntProtoId Product;

    public override bool IsValid()
    {
        return IoCManager.Resolve<IPrototypeManager>().HasIndex(Product);
    }

    public override EntityUid? FulfillOrder(Entity<StationDataComponent> stationData, ProtoId<CargoAccountPrototype> account, CargoOrderData order, StationCargoOrderDatabaseComponent orderDatabase)
    {
        EntityUid? tradeDestination = null;

        var entity = IoCManager.Resolve<IEntityManager>();
        var random = IoCManager.Resolve<IRobustRandom>();

        var cargo = entity.System<CargoSystem>();
        var slots = entity.System<ItemSlotsSystem>();
        var transform = entity.System<TransformSystem>();

        // Try to fulfill from any station where possible, if the pad is not occupied.
        foreach (var trade in cargo.GetTradeStations(stationData))
        {
            var tradePads = cargo.GetCargoPallets(trade, BuySellType.Buy);
            random.Shuffle(tradePads);

            var freePads = cargo.GetFreeCargoPallets(trade, tradePads);
            if (freePads.Count >= order.OrderQuantity) //check if the station has enough free pallets
            {
                foreach (var (pad, _, padXform) in freePads)
                {
                    var item = entity.SpawnEntity(order.ProductId, padXform.Coordinates);

                    // Ensure the item doesn't start anchored
                    transform.Unanchor(item);

                    // Create a sheet of paper to write the order details on
                    var printed = cargo.SpawnPaper(order, account, orderDatabase.PrinterOutput);

                    // attempt to attach the label to the item
                    if (entity.TryGetComponent<PaperLabelComponent>(item, out var label))
                        slots.TryInsert(item, label.LabelSlot, printed, null);

                    tradeDestination = trade;
                    order.NumDispatched++;
                    if (order.OrderQuantity <= order.NumDispatched) //Spawn a crate on free pellets until the order is fulfilled.
                        break;
                }
            }

            if (tradeDestination != null)
                break;
        }

        return tradeDestination;
    }
}
