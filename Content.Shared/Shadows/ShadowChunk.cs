using Robust.Shared.Serialization;

namespace Content.Shared.Shadows;

[Serializable, NetSerializable]
public readonly struct ShadowChunk(Vector2i chunkPos, int estimatedCapacity = 64)
{
    public readonly Vector2i ChunkPos = chunkPos;
    public readonly Dictionary<Vector2i, ShadowData> ShadowMap = new(estimatedCapacity);
}
