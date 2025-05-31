using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Modules.Components.Modules;

[RegisterComponent, NetworkedComponent]
public sealed partial class ToggleableComponentContainerModuleComponent : Component
{
    [DataField]
    public ComponentRegistry Components;
}
