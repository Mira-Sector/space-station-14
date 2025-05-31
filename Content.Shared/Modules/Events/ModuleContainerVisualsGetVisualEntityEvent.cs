namespace Content.Shared.Modules.Events;

public sealed partial class ModuleContainerVisualsGetVisualEntityEvent : EntityEventArgs
{
    public EntityUid? Entity;
}
