namespace Content.Shared.Modules.Events;

public sealed partial class ModuleContainerModuleAddedEvent(EntityUid module) : BaseModuleContainerModifyEvent(module);
