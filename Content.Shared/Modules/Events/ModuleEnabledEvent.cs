namespace Content.Shared.Modules.Events;

public sealed partial class ModuleEnabledEvent(EntityUid container, EntityUid? user) : BaseModuleToggleEvent(container, user);
