using Robust.Shared.Prototypes;

namespace Content.Shared.Modules.Components.Modules;

public abstract partial class BaseToggleableModuleComponent : BaseModuleComponent
{
    [DataField]
    public EntProtoId? ActionId;

    [ViewVariables]
    public EntityUid? Action;

    [ViewVariables]
    public bool Toggled;
}
