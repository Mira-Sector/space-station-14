using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.Sync.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause, Access(typeof(SharedSiliconSyncSystem))]
public sealed partial class SiliconSyncableMasterCommanderComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Commanding;

    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextCommand;
}
