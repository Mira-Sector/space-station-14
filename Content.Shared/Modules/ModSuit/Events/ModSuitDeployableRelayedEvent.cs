namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitDeployableRelayedEvent<T> : EntityEventArgs
{
    public T Args;
    public readonly EntityUid ModSuit;

    public ModSuitDeployableRelayedEvent(T args, EntityUid modSuit)
    {
        Args = args;
        ModSuit = modSuit;
    }
}
