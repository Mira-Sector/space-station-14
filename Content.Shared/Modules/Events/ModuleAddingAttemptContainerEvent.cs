namespace Content.Shared.Modules.Events;

public sealed partial class ModuleAddingAttemptContainerEvent : CancellableEntityEventArgs
{
    public EntityUid Container;

    public ModuleAddingAttemptContainerEvent(EntityUid container)
    {
        Container = container;
    }
}
