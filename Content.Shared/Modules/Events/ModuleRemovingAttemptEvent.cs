namespace Content.Shared.Modules.Events;

public sealed partial class ModuleRemovingAttemptEvent(EntityUid container) : BaseModuleModifyAttemptEvent(container);
