namespace Content.Shared.Modules.Components.Modules;

public abstract partial class BaseModuleComponent : Component
{
    [DataField]
    public EntityUid? Container;
}
