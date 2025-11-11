namespace Content.Shared.Modules.Events;

public sealed partial class ModuleContainerModuleRemovedEvent(EntityUid module) : BaseModuleContainerModifyEvent(module);
