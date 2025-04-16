using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.Sync.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SiliconSyncSystem))]
public sealed partial class SiliconSyncableMasterComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> Slaves = new();
}
