using Content.Shared.Modules.ModSuit;
using Robust.Shared.GameStates;

namespace Content.Shared.Modules.Components.Modules;

[RegisterComponent, NetworkedComponent]
public sealed partial class MagbootsModuleComponent : Component
{
    [DataField]
    public ModSuitPartType? ModSuitPart;
}
