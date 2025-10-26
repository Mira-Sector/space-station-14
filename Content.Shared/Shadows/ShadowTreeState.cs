using Robust.Shared.Serialization;

namespace Content.Shared.Shadows;

[Serializable, NetSerializable]
public sealed class ShadowTreeState(HashSet<NetEntity> casters, Dictionary<Vector2i, ShadowChunk> chunks) : ComponentState
{
    public readonly HashSet<NetEntity> Casters = casters;

    public readonly Dictionary<Vector2i, ShadowChunk> Chunks = chunks;
}
