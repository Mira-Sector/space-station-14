using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo.Orders;

public abstract partial class SharedCargoProductItemData : BaseCargoProductData
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

    public override string? GetName()
    {
        if (IoCManager.Resolve<IPrototypeManager>().TryIndex(Product, out var entity))
            return entity.Name;

        return base.GetName();
    }

    public override string? GetDescription()
    {
        if (IoCManager.Resolve<IPrototypeManager>().TryIndex(Product, out var entity))
            return entity.Description;

        return base.GetDescription();
    }
}
