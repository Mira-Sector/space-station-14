namespace Content.Shared.Modules.Events;

public sealed partial class ModuleContainerModuleAddingAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Module;
    public LocId? Reason;

    public ModuleContainerModuleAddingAttemptEvent(EntityUid module)
    {
        Module = module;
    }
}
