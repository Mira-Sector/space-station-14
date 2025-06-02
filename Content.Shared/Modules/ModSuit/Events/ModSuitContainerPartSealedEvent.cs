namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitContainerPartSealedEvent : BaseModSuitContainerSealEvent
{
    public ModSuitContainerPartSealedEvent(EntityUid part) : base(part)
    {
    }
}
