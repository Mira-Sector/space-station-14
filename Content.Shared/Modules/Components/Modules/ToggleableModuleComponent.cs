using Robust.Shared.GameStates;

namespace Content.Shared.Modules.Components.Modules;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToggleableModuleComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public bool Toggled;
}
