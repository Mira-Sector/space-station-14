namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitDeployedPartRelayedEvent<T> : EntityEventArgs
{
    public T Args;
    public readonly EntityUid Part;

    public ModSuitDeployedPartRelayedEvent(T args, EntityUid part)
    {
        Args = args;
        Part = part;
    }
}
