using Content.Shared.Store.Systems;
using Content.Shared.Store.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Server.Cargo.Components;
using Content.Server.Station.Systems;
using Content.Server.Chat.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Store.Systems;

public sealed partial class StorePurchaseEventSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreComponent, StorePurchaseAddCargoProductEvent>(OnStorePurchaseAddCargoProduct);

    }

    private void OnStorePurchaseAddCargoProduct(Entity<StoreComponent> ent, ref StorePurchaseAddCargoProductEvent args)
    {
        var station = _station.GetOwningStation(ent) ?? EntityUid.Invalid;
        if (TryComp<StationCargoOrderDatabaseComponent>(station, out var stationCargoOrder))
        {
            if (!stationCargoOrder.Markets.Contains(args.Market))
                stationCargoOrder.Markets.Add(args.Market);

            var cargoProduct = _proto.Index(args.CargoProduct);
            //cargoProduct.Group = args.Market;
            Log.Debug("StorePurchaseAddCargoProduct Bought");
        }

        Log.Debug("StorePurchaseAddCargoProduct Ran");

    }
}
