namespace Content.Shared.Modules.Events;

public sealed partial class ModuleContainerRelayedEvent<T>(T args, EntityUid module) : EntityEventArgs
{
    public T Args = args;
    public readonly EntityUid Module = module;
}
