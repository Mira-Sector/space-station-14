using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.Sync.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SiliconSyncableSlaveLawComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public bool Enabled = true;
}
