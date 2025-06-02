using Robust.Shared.GameStates;

namespace Content.Shared.Modules.ModSuit.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModSuitModuleComplexityComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Complexity;
}
