namespace Content.Shared.Modules.Events;

public abstract partial class BaseModuleToggleEvent : EntityEventArgs
{
    public readonly EntityUid Container;
    public readonly EntityUid? User;

    public BaseModuleToggleEvent(EntityUid container, EntityUid? user)
    {
        Container = container;
        User = user;
    }
}
