using Robust.Shared.GameStates;

namespace Content.Shared.Holodeck.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HolodeckSpawnedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid SpawnedBy;
}
