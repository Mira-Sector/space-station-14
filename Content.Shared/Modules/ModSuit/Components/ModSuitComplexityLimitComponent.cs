using Robust.Shared.GameStates;

namespace Content.Shared.Modules.ModSuit.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModSuitComplexityLimitComponent : Component
{
    [DataField, AutoNetworkedField]
    public int MaxComplexity;

    [ViewVariables, AutoNetworkedField]
    public int Complexity;
}
