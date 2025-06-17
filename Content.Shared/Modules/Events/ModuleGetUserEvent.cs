namespace Content.Shared.Modules.Events;

public sealed partial class ModuleGetUserEvent : EntityEventArgs
{
    public EntityUid? User;
}
