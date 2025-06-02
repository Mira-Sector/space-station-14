namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitDeployablePartDeployedEvent : EntityEventArgs
{
    public readonly EntityUid Suit;
    public readonly EntityUid Wearer;
    public readonly string Slot;

    public ModSuitDeployablePartDeployedEvent(EntityUid suit, EntityUid wearer, string slot)
    {
        Suit = suit;
        Wearer = wearer;
        Slot = slot;
    }
}
