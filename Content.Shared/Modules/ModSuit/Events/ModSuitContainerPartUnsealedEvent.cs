namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitContainerPartUnsealedEvent : BaseModSuitContainerSealEvent
{
    public ModSuitContainerPartUnsealedEvent(EntityUid part) : base(part)
    {
    }
}
