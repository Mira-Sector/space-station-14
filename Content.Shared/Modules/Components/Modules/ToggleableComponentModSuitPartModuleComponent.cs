using Content.Shared.Modules.ModSuit;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Modules.Components.Modules;

[RegisterComponent, NetworkedComponent]
public sealed partial class ToggleableComponentModSuitPartModuleComponent : Component
{
    [DataField]
    public ComponentRegistry Components;

    [DataField("part", required: true)]
    public ModSuitPartType PartType;
}
