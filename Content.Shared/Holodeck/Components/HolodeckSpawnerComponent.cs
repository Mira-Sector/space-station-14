using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Holodeck.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HolodeckSpawnerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityCoordinates Center;

    [DataField, AutoNetworkedField]
    public List<EntityUid> Spawned = [];
}
