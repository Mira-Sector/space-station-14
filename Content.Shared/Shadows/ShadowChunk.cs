using Robust.Shared.Serialization;

namespace Content.Shared.Shadows;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ShadowChunk
{
    [ViewVariables]
    public readonly Vector2i ChunkPos;

    [ViewVariables]
    public readonly Dictionary<Vector2i, ShadowData> ShadowMap;

    public ShadowChunk(Vector2i chunkPos, int estimatedCapacity = 64)
    {
        ChunkPos = chunkPos;
        ShadowMap = new(estimatedCapacity);
    }
}
