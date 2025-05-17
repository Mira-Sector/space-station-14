namespace Content.Shared.Modules.Events;

public sealed partial class ModuleContainerModuleRemovedEvent : EntityEventArgs
{
    public EntityUid Module;

    public ModuleContainerModuleRemovedEvent(EntityUid module)
    {
        Module = module;
    }
}

