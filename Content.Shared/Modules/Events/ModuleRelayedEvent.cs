namespace Content.Shared.Modules.Events;

public sealed partial class ModuleRelayedEvent<T> : EntityEventArgs
{
    public T Args;
    public readonly EntityUid ModuleOwner;

    public ModuleRelayedEvent(T args, EntityUid moduleOwner)
    {
        Args = args;
        ModuleOwner = moduleOwner;
    }
}
