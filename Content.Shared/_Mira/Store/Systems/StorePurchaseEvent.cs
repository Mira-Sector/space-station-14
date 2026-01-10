using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Store.Systems;


/// <summary>
/// Can be raised on the purchase of something from a store
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class StorePurchaseEvent : EntityEventArgs
{
    /// <summary>
    /// EntityUid of entity that acts as a store
    /// </summary>
    public EntityUid Location;
}

/// <summary>
/// Can be raised on the purchase of something from a store, as a result then adds something to a cargo market
/// </summary>
public sealed partial class StorePurchaseAddCargoProductEvent : StorePurchaseEvent
{
    /// <summary>
    /// Product to add
    /// </summary>
    [DataField(required: true)]
    public string CargoProduct = "MiraCargoCrusher";

    /// <summary>
    /// Market to add the cargo product to
    /// </summary>
    [DataField]
    public string Market = "unlockedSalvage";

    /// <summary>
    /// Message of unlock
    /// </summary>
    [DataField]
    public string Message = "store-purchase-unlock-null";

    /// <summary>
    /// Whether to also gift the item, sends it to the ATS if possible
    /// </summary>
    [DataField]
    public bool GiftProduct = true;
}

