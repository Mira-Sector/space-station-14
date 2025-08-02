namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitDeployedPartRelayedEvent<T>(T args, EntityUid part) : EntityEventArgs
{
    public T Args = args;
    public readonly EntityUid Part = part;
}
