using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.Sync.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedSiliconSyncSystem))]
public sealed partial class SiliconSyncableMasterComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> Slaves = new();
}
