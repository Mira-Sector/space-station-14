using Robust.Shared.GameStates;

namespace Content.Shared.Shadows.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedShadowSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class ShadowTreeComponent : Component
{
    public const int ChunkSize = 16;

    [ViewVariables]
    public HashSet<EntityUid> Casters = [];

    [ViewVariables]
    public Dictionary<Vector2i, ShadowChunk> Chunks = [];
}
