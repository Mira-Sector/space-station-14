namespace Content.Shared.Modules.Events;

public sealed partial class ModuleRemovedContainerEvent(EntityUid container) : BaseModuleModifyEvent(container);
