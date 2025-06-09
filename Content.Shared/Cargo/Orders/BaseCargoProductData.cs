namespace Content.Shared.Cargo.Orders;

[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseCargoProductData
{
    public virtual bool IsValid()
    {
        return true;
    }

    public virtual string? GetName()
    {
        return null;
    }

    public virtual string? GetDescription()
    {
        return null;
    }
}
