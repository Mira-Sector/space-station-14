using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Modules.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ModuleContainerComponent : Component
{
    [DataField]
    public string ModuleContainerId = "modules";

    [ViewVariables]
    public Container Modules = new();
}
