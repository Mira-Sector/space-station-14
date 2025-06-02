using Robust.Shared.GameStates;

namespace Content.Shared.Modules.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModuleContainerPowerComponent : Component
{
    [DataField, AutoNetworkedField]
    public float BaseRate;
}
