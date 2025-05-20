namespace Content.Shared.Modules.Events;

public sealed partial class ModuleDisabledEvent : BaseModuleToggleEvent
{
    public ModuleDisabledEvent(EntityUid container, EntityUid? user) : base(container, user)
    {
    }
}
