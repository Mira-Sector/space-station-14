namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitDeployablePartUnequippedEvent : EntityEventArgs
{
    public readonly EntityUid Suit;
    public readonly EntityUid? Wearer;
    public readonly string Slot;

    public ModSuitDeployablePartUnequippedEvent(EntityUid suit, EntityUid? wearer, string slot)
    {
        Suit = suit;
        Wearer = wearer;
        Slot = slot;
    }
}
