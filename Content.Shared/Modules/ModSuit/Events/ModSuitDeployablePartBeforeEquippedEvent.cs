namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitDeployablePartBeforeEquippedEvent : EntityEventArgs
{
    public readonly EntityUid Suit;
    public readonly EntityUid Wearer;
    public readonly string Slot;
    public readonly int PartNumber;

    public ModSuitDeployablePartBeforeEquippedEvent(EntityUid suit, EntityUid wearer, string slot, int partNumber)
    {
        Suit = suit;
        Wearer = wearer;
        Slot = slot;
        PartNumber = partNumber;
    }
}
