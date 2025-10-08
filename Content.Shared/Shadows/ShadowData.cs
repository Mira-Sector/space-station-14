using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Shadows;

[Serializable, NetSerializable]
public readonly record struct ShadowData(Vector2 Direction, float Strength)
{
    public static readonly ShadowData Empty = new(Vector2.Zero, 0f);

    public const float MinStrength = 0.001f;

    public static ShadowData Combine(ShadowData a, ShadowData b)
    {
        var totalStrength = a.Strength + b.Strength;
        if (totalStrength < MinStrength)
            return Empty;

        // weighted direction average
        var weightedDir = (a.Direction * a.Strength + b.Direction * b.Strength) / totalStrength;
        var dirNorm = weightedDir.Normalized();

        var strength = MathF.Min(totalStrength, 1f);
        return new ShadowData(dirNorm, strength);
    }

    public static ShadowData Lerp(ShadowData a, ShadowData b, float t)
    {
        var dir = Vector2.Lerp(a.Direction, b.Direction, t);
        var str = MathHelper.Lerp(a.Strength, b.Strength, t);
        return new ShadowData(dir, str);
    }
}
