namespace Content.Shared.Modules.Events;

public sealed partial class ModuleContainerRelayedEvent<T> : EntityEventArgs
{
    public T Args;
    public readonly EntityUid Module;

    public ModuleContainerRelayedEvent(T args, EntityUid module)
    {
        Args = args;
        Module = module;
    }
}
