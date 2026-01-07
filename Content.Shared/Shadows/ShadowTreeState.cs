using Robust.Shared.Serialization;

namespace Content.Shared.Shadows;

[Serializable, NetSerializable]
public sealed partial class ShadowTreeState(Dictionary<Vector2i, ShadowChunk> chunks) : ComponentState
{
    public readonly Dictionary<Vector2i, ShadowChunk> Chunks = chunks;
}
