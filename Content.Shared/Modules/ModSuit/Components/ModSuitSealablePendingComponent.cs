using Robust.Shared.GameStates;

namespace Content.Shared.Modules.ModSuit.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ModSuitSealablePendingComponent : Component
{
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate;

    [ViewVariables, AutoNetworkedField]
    public bool ShouldSeal;
}
