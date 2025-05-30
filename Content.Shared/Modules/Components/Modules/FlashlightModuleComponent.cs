using Content.Shared.Modules.Modules;
using Robust.Shared.GameStates;

namespace Content.Shared.Modules.Components.Modules;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FlashlightModuleComponent : BaseToggleableUiModuleComponent
{
    [DataField, AutoNetworkedField, Access(typeof(FlashlightModuleSystem))]
    public Color Color = Color.White;
}
