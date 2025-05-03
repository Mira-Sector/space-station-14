namespace Content.Shared.Modules.Events;

public sealed partial class ModuleContainerModuleAddedEvent : EntityEventArgs
{
    public EntityUid Module;

    public ModuleContainerModuleAddedEvent(EntityUid module)
    {
        Module = module;
    }
}
