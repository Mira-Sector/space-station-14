namespace Content.Shared.Modules.Events;

public abstract partial class BaseModuleContainerModifyAttemptEvent(EntityUid module) : CancellableEntityEventArgs
{
    public readonly EntityUid Module = module;
    public LocId? Reason;
}
