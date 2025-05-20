namespace Content.Shared.Modules.Events;

public sealed partial class ModuleToggleAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Container;
    public readonly EntityUid? User;

    public ModuleToggleAttemptEvent(EntityUid container, EntityUid? user)
    {
        Container = container;
        User = user;
    }
}
