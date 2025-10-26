using Robust.Shared.ComponentTrees;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;

namespace Content.Shared.Shadows.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedShadowSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class ShadowTreeComponent : Component, IComponentTreeComponent<ShadowCasterComponent>
{
    public const int ChunkSize = 16;

    [ViewVariables]
    public DynamicTree<ComponentTreeEntry<ShadowCasterComponent>> Tree { get; set; }

    [ViewVariables]
    public Dictionary<Vector2i, ShadowChunk> Chunks = [];
}
