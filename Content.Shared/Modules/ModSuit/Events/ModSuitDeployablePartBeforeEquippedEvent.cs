namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitDeployablePartBeforeEquippedEvent : EntityEventArgs
{
    public EntityUid Suit { get; private set; }
    public EntityUid Wearer { get; private set; }
    public string Slot { get; private set; }

    public ModSuitDeployablePartBeforeEquippedEvent(EntityUid suit, EntityUid wearer, string slot)
    {
        Suit = suit;
        Wearer = wearer;
        Slot = slot;
    }
}
