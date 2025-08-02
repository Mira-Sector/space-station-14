namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitDeployableRelayedEvent<T>(T args, EntityUid modSuit) : EntityEventArgs
{
    public T Args = args;
    public readonly EntityUid ModSuit = modSuit;
}
