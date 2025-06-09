namespace Content.Shared.Modules.ModSuit.Events;

public abstract partial class BaseModSuitContainerSealEvent : EntityEventArgs
{
    public readonly EntityUid Part;

    public BaseModSuitContainerSealEvent(EntityUid part)
    {
        Part = part;
    }
}
