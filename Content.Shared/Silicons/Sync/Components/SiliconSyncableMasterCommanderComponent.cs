using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.Sync.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedSiliconSyncSystem))]
public sealed partial class SiliconSyncableMasterCommanderComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Commanding;
}
