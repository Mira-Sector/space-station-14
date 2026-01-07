using Robust.Shared.Serialization;

namespace Content.Shared.Maths;

[Serializable, NetSerializable]
public partial struct Box3Rotated : IEquatable<Box3Rotated>, IApproxEquatable<Box3Rotated>
{
    [ViewVariables]
    public Box3 Box;

    [ViewVariables]
    public Quaternion Quaternion;

    [ViewVariables]
    public Vector3 Origin;

    public static readonly Box3Rotated Empty = new(Box3.Empty);
    public static readonly Box3Rotated UnitCentered = new(Box3.UnitCentered);

    public readonly Vector3 LeftBottomBack => Origin + Vector3.Transform(Box.LeftBottomBack - Box.Center, Quaternion);
    public readonly Vector3 LeftBottomFront => Origin + Vector3.Transform(Box.LeftBottomFront - Box.Center, Quaternion);
    public readonly Vector3 LeftTopBack => Origin + Vector3.Transform(Box.LeftTopBack - Box.Center, Quaternion);
    public readonly Vector3 LeftTopFront => Origin + Vector3.Transform(Box.LeftTopFront - Box.Center, Quaternion);
    public readonly Vector3 RightBottomBack => Origin + Vector3.Transform(Box.RightBottomBack - Box.Center, Quaternion);
    public readonly Vector3 RightBottomFront => Origin + Vector3.Transform(Box.RightBottomFront - Box.Center, Quaternion);
    public readonly Vector3 RightTopBack => Origin + Vector3.Transform(Box.RightTopBack - Box.Center, Quaternion);
    public readonly Vector3 RightTopFront => Origin + Vector3.Transform(Box.RightTopFront - Box.Center, Quaternion);

    public readonly Vector3[] GetCorners()
    {
        var halfSize = Box.Size * 0.5f;
        var signs = new Vector3[]
        {
            new(-1, -1, -1),
            new(1, -1, -1),
            new(1, 1, -1),
            new(1, 1, -1),
            new(1, -1, 1),
            new(1, -1, 1),
            new(1, 1, 1),
            new(1, 1, 1),
        };

        var corners = new Vector3[signs.Length];
        for (var i = 0; i < corners.Length; i++)
        {
            var local = signs[i] * halfSize;
            corners[i] = Origin + Vector3.Transform(local, Quaternion);
        }

        return corners;
    }

    public readonly Vector3[] GetLocalAxes()
    {
        return new[]
        {
            Vector3.Transform(Vector3.UnitX, Quaternion),
            Vector3.Transform(Vector3.UnitY, Quaternion),
            Vector3.Transform(Vector3.UnitZ, Quaternion),
        };
    }

    public readonly Box3 CalcBoundingBox()
    {
        var corners = GetCorners();
        var min = corners[0];
        var max = corners[0];
        for (var i = 1; i < corners.Length; i++)
        {
            min = Vector3.ComponentMin(min, corners[i]);
            max = Vector3.ComponentMax(max, corners[i]);
        }
        return new Box3(min, max);
    }

    public readonly bool Contains(Vector3 point)
    {
        var local = TransformToLocal(point) - Box.Center;
        var halfSize = Box.Size * 0.5f;

        return Math.Abs(local.X) <= halfSize.X &&
               Math.Abs(local.Y) <= halfSize.Y &&
               Math.Abs(local.Z) <= halfSize.Z;
    }

    public readonly bool Contains(Box3Rotated other)
    {
        var aabb1 = CalcBoundingBox();
        var aabb2 = other.CalcBoundingBox();
        return aabb1.Contains(aabb2);
    }

    public readonly Box3Rotated Translate(Vector3 offset)
    {
        return new Box3Rotated(Box, Quaternion, Origin + offset);
    }

    public readonly Box3Rotated Rotate(Quaternion rotation)
    {
        var newRot = Quaternion.Normalize(rotation * Quaternion);
        return new Box3Rotated(Box, newRot, Origin);
    }

    public readonly Box3Rotated RotateAround(Quaternion rotation, Vector3 pivot)
    {
        var newOrigin = pivot + Vector3.Transform(Origin - pivot, rotation);
        var newRot = Quaternion.Normalize(rotation * Quaternion);
        return new Box3Rotated(Box, newRot, newOrigin);
    }

    public readonly Vector3 TransformToLocal(Vector3 point)
    {
        var translated = point - Origin;

        var invRot = Quaternion.Invert(Quaternion);
        var local = Vector3.Transform(translated, invRot);

        return local + Box.Center;
    }

    public readonly bool EqualsApprox(Box3Rotated other)
    {
        return Box.EqualsApprox(other.Box) &&
            MathHelper.CloseToPercent(Math.Abs(Quaternion.Dot(Quaternion, other.Quaternion)), 1f);
    }

    public readonly bool EqualsApprox(Box3Rotated other, double tolerance)
    {
        return Box.EqualsApprox(other.Box) &&
            MathHelper.CloseToPercent(Math.Abs(Quaternion.Dot(Quaternion, other.Quaternion)), 1f, tolerance);
    }

    public Box3Rotated(Box3 box, Quaternion quaternion, Vector3 origin)
    {
        Box = box;
        Quaternion = quaternion;
        Origin = origin;
    }

    public Box3Rotated(Box3 box, Quaternion quaternion)
    {
        Box = box;
        Quaternion = quaternion;
        Origin = box.Center;
    }

    public Box3Rotated(Box3 box, Vector3 origin)
    {
        Box = box;
        Quaternion = Quaternion.Identity;
        Origin = origin;
    }

    public Box3Rotated(Box3 box)
    {
        Box = box;
        Quaternion = Quaternion.Identity;
        Origin = box.Center;
    }

    public static implicit operator Box3Rotated(Box3 n) => new(n);

    public readonly bool Equals(Box3Rotated other)
    {
        return Box.Equals(other.Box) &&
            Quaternion.Equals(other.Quaternion);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Box3Rotated other && Equals(other);
    }

    public static bool operator ==(Box3Rotated a, Box3Rotated b) => a.EqualsApprox(b);

    public static bool operator !=(Box3Rotated a, Box3Rotated b) => !a.EqualsApprox(b);

    public override readonly int GetHashCode() => HashCode.Combine(Box, Quaternion);

    public override readonly string ToString()
    {
        return $"{Box} {Quaternion}";
    }
}
