using Robust.Shared.GameStates;

namespace Content.Shared.Shadows.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedShadowSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class ShadowTreeComponent : Component
{
    public const int ChunkSize = 16;

    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> Casters = [];

    [ViewVariables, AutoNetworkedField]
    public Dictionary<Vector2i, ShadowChunk> Chunks = [];
}
