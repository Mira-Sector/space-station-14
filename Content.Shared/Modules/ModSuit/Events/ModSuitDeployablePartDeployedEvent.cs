namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitDeployablePartDeployedEvent : EntityEventArgs
{
    public readonly EntityUid Suit;
    public readonly EntityUid Wearer;
    public readonly string Slot;
    public readonly int PartNumber;

    public ModSuitDeployablePartDeployedEvent(EntityUid suit, EntityUid wearer, string slot, int partNumber)
    {
        Suit = suit;
        Wearer = wearer;
        Slot = slot;
        PartNumber = partNumber;
    }
}
