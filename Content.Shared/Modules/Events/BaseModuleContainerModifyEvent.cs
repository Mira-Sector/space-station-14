namespace Content.Shared.Modules.Events;

public abstract partial class BaseModuleContainerModifyEvent(EntityUid module) : EntityEventArgs
{
    public readonly EntityUid Module = module;
}
