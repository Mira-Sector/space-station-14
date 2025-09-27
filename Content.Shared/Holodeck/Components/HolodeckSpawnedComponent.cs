using Robust.Shared.GameStates;

namespace Content.Shared.Holodeck.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HolodeckSpawnedComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid SpawnedBy;

    [ViewVariables, AutoNetworkedField]
    public List<Box2i>? HolodeckBounds;
}
