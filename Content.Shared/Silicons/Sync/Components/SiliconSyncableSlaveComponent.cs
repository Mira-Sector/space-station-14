using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.Sync.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SiliconSyncSystem))]
public sealed partial class SiliconSyncableSlaveComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Master;
}
