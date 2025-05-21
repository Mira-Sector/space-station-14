namespace Content.Shared.Modules.Events;

public sealed partial class ModuleToggleAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Container;
    public readonly EntityUid? User;
    public readonly bool Toggle;
    public LocId Reason = "module-toggle-failed-generic";

    public ModuleToggleAttemptEvent(bool toggle, EntityUid container, EntityUid? user)
    {
        Toggle = toggle;
        Container = container;
        User = user;
    }
}
