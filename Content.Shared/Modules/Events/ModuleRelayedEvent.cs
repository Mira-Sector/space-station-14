namespace Content.Shared.Modules.Events;

public sealed partial class ModuleRelayedEvent<T>(T args, EntityUid moduleOwner) : EntityEventArgs
{
    public T Args = args;
    public readonly EntityUid ModuleOwner = moduleOwner;
}
