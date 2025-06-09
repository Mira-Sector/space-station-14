namespace Content.Shared.Modules.Events;

public sealed partial class ModuleContainerModuleRemovedEvent : EntityEventArgs
{
    public readonly EntityUid Module;

    public ModuleContainerModuleRemovedEvent(EntityUid module)
    {
        Module = module;
    }
}
