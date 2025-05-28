using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Modules.Components.Modules;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerDrainModuleComponent : BaseToggleableModuleComponent
{
    [DataField, AutoNetworkedField]
    public float? EnabledDraw;

    [DataField, AutoNetworkedField]
    public float? DisabledDraw;

    [DataField, AutoNetworkedField]
    public float? OnUseDraw;
}
