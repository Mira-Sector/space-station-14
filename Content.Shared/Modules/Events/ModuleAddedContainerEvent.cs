namespace Content.Shared.Modules.Events;

public sealed partial class ModuleAddedContainerEvent(EntityUid container) : BaseModuleModifyEvent(container);
