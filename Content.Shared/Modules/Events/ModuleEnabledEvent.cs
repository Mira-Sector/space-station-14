namespace Content.Shared.Modules.Events;

public sealed partial class ModuleEnabledEvent : BaseModuleToggleEvent
{
    public ModuleEnabledEvent(EntityUid container, EntityUid? user) : base(container, user)
    {
    }
}
