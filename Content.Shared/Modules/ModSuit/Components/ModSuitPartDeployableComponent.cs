using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Modules.ModSuit.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModSuitModulePartDeployableComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, EntProtoId> DeployableParts = [];

    [ViewVariables, AutoNetworkedField, Access(typeof(SharedModSuitSystem))]
    public HashSet<EntityUid> DeployedParts = [];
}
