namespace Content.Shared.Modules.Events;

public sealed partial class ModuleAddingAttemptContainerEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Container;
    public LocId? Reason;

    public ModuleAddingAttemptContainerEvent(EntityUid container)
    {
        Container = container;
    }
}
