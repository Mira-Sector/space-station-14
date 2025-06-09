using Robust.Shared.Utility;

namespace Content.Shared.Cargo.Orders;

public abstract partial class SharedCargoProductShuttleData : BaseCargoProductData
{
    [DataField]
    public ResPath Shuttle;
}
