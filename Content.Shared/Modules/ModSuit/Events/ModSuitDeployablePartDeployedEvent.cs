namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitDeployablePartDeployedEvent : EntityEventArgs
{
    public EntityUid Suit { get; private set; }
    public EntityUid Wearer { get; private set; }
    public string Slot { get; private set; }

    public ModSuitDeployablePartDeployedEvent(EntityUid suit, EntityUid wearer, string slot)
    {
        Suit = suit;
        Wearer = wearer;
        Slot = slot;
    }
}
