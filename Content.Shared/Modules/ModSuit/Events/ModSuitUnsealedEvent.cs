namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitUnsealedEvent : BaseModSuitSealEvent
{
    public ModSuitUnsealedEvent(EntityUid? wearer) : base(wearer)
    {
    }
}
