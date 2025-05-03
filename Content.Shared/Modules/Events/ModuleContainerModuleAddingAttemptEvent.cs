namespace Content.Shared.Modules.Events;

public sealed partial class ModuleContainerModuleAddingAttemptEvent : CancellableEntityEventArgs
{
    public EntityUid Module;

    public ModuleContainerModuleAddingAttemptEvent(EntityUid module)
    {
        Module = module;
    }
}
