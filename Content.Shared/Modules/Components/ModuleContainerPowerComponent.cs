using Robust.Shared.GameStates;

namespace Content.Shared.Modules.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ModuleContainerPowerComponent : Component
{
    [DataField]
    public float BaseRate;
}
