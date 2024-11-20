using Robust.Shared.GameStates;

namespace Content.Shared.Wounds.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class WoundBodyComponent : Component
{
    [ViewVariables]
    public List<EntityUid> Limbs = new();
}
