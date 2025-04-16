using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.Sync.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedSiliconSyncSystem))]
public sealed partial class SiliconSyncableSlaveComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public bool Enabled = true;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Master;
}
