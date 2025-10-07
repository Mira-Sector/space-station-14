using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Shadows;

[Serializable, NetSerializable]
public readonly record struct ShadowData(Vector2 Direction, float Strength);
