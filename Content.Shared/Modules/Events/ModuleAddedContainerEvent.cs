namespace Content.Shared.Modules.Events;

public sealed partial class ModuleAddedContainerEvent : EntityEventArgs
{
    public readonly EntityUid Container;

    public ModuleAddedContainerEvent(EntityUid container)
    {
        Container = container;
    }
}
