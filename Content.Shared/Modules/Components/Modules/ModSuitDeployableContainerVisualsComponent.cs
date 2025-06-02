using Content.Shared.Modules.ModSuit;
using Robust.Shared.GameStates;

namespace Content.Shared.Modules.Components.Modules;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModSuitDeployableContainerVisualsComponent : Component
{
    [DataField, AutoNetworkedField]
    public ModSuitPartType PartType;
}
