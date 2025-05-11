namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitDeployablePartUndeployedEvent : EntityEventArgs
{
    public EntityUid Suit { get; private set; }
    public EntityUid Wearer { get; private set; }

    public ModSuitDeployablePartUndeployedEvent(EntityUid suit, EntityUid wearer)
    {
        Suit = suit;
        Wearer = wearer;
    }
}
