namespace Content.Shared.Modules.Events;

public sealed partial class ModuleRemovedContainerEvent : EntityEventArgs
{
    public EntityUid Container;

    public ModuleRemovedContainerEvent(EntityUid container)
    {
        Container = container;
    }
}
