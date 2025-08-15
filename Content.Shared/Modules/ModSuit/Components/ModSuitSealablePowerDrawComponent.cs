using Robust.Shared.GameStates;

namespace Content.Shared.Modules.ModSuit.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ModSuitSealablePowerDrawComponent : Component
{
    [DataField]
    public Dictionary<bool, float> PowerDraw = [];
}
