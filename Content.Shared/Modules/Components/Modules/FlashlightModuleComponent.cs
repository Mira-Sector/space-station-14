using Content.Shared.Modules.Modules;
using Robust.Shared.GameStates;

namespace Content.Shared.Modules.Components.Modules;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FlashlightModuleComponent : Component
{
    [DataField, AutoNetworkedField, Access(typeof(SharedFlashlightModuleSystem))]
    public Color Color = Color.White;
}
