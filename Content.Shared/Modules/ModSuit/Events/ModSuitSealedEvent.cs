namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitSealedEvent : BaseModSuitSealEvent
{
    public ModSuitSealedEvent(EntityUid? wearer) : base(wearer)
    {
    }
}
