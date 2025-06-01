namespace Content.Shared.Modules.ModSuit.Events;

public abstract partial class BaseModSuitSealEvent : EntityEventArgs
{
    public readonly EntityUid? Wearer;

    public BaseModSuitSealEvent(EntityUid? wearer)
    {
        Wearer = wearer;
    }
}
