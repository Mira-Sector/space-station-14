namespace Content.Shared.Modules.Events;

public sealed partial class ModuleDisabledEvent(EntityUid container, EntityUid? user) : BaseModuleToggleEvent(container, user);
