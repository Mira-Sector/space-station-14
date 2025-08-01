namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitDeployablePartBeforeEquippedEvent(EntityUid suit, EntityUid wearer, string slot, int partNumber) : EntityEventArgs
{
    public readonly EntityUid Suit = suit;
    public readonly EntityUid Wearer = wearer;
    public readonly string Slot = slot;
    public readonly int PartNumber = partNumber;
}
