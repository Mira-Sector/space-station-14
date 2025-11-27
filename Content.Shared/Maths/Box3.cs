using Robust.Shared.Serialization;
using System.Numerics;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Shared.Maths;

[Serializable, NetSerializable]
public partial struct Box3 : IEquatable<Box3>, IApproxEquatable<Box3>
{
    [ViewVariables]
    public float Left;

    [ViewVariables]
    public float Bottom;

    [ViewVariables]
    public float Back;

    [ViewVariables]
    public float Right;

    [ViewVariables]
    public float Top;

    [ViewVariables]
    public float Front;

    public static readonly Box3 Empty = new(0f);
    public static readonly Box3 UnitCentered = new(0.5f);

    public readonly Vector3 LeftBottomBack => new(Left, Bottom, Back);
    public readonly Vector3 LeftBottomFront => new(Left, Bottom, Front);
    public readonly Vector3 LeftTopBack => new(Left, Top, Back);
    public readonly Vector3 LeftTopFront => new(Left, Top, Front);
    public readonly Vector3 RightBottomBack => new(Right, Bottom, Back);
    public readonly Vector3 RightBottomFront => new(Right, Bottom, Front);
    public readonly Vector3 RightTopBack => new(Right, Top, Back);
    public readonly Vector3 RightTopFront => new(Right, Top, Front);

    public readonly Vector2 LeftBottom => new(Left, Bottom);
    public readonly Vector2 RightTop => new(Right, Top);

    public readonly Vector2 LeftBack => new(Left, Back);
    public readonly Vector2 RightFront => new(Right, Front);

    public readonly Vector2 BottomBack => new(Bottom, Back);
    public readonly Vector2 TopFront => new(Top, Front);

    public readonly Box2 XY => new(Left, Bottom, Right, Top);
    public readonly Box2 XZ => new(Left, Back, Right, Front);
    public readonly Box2 YZ => new(Bottom, Back, Top, Front);

    public readonly Vector3 Center => (LeftBottomBack + RightTopFront) * 0.5f;

    public readonly Vector3 Size => RightTopFront - LeftBottomBack;

    public readonly bool Contains(Vector3 point)
    {
        return point.X >= Left && point.X <= Right &&
            point.Y >= Bottom && point.Y <= Top &&
            point.Z >= Back && point.Z <= Front;
    }

    public readonly bool Contains(Box3 other)
    {
        return Left <= other.Left && Right >= other.Right &&
           Bottom <= other.Bottom && Top >= other.Top &&
           Back <= other.Back && Front >= other.Front;
    }

    public readonly bool Intersects(Box3 other)
    {
        return Left <= other.Right && Right >= other.Left &&
            Bottom <= other.Top && Top >= other.Bottom &&
            Back <= other.Front && Front >= other.Back;
    }

    public readonly bool EqualsApprox(Box3 other)
    {
        return MathHelper.CloseToPercent(Left, other.Left) &&
            MathHelper.CloseToPercent(Bottom, other.Bottom) &&
            MathHelper.CloseToPercent(Back, other.Back) &&
            MathHelper.CloseToPercent(Right, other.Right) &&
            MathHelper.CloseToPercent(Top, other.Top) &&
            MathHelper.CloseToPercent(Front, other.Front);
    }

    public readonly bool EqualsApprox(Box3 other, double tolerance)
    {
        return MathHelper.CloseToPercent(Left, other.Left, tolerance) &&
            MathHelper.CloseToPercent(Bottom, other.Bottom, tolerance) &&
            MathHelper.CloseToPercent(Back, other.Back, tolerance) &&
            MathHelper.CloseToPercent(Right, other.Right, tolerance) &&
            MathHelper.CloseToPercent(Top, other.Top, tolerance) &&
            MathHelper.CloseToPercent(Front, other.Front, tolerance);
    }

    public readonly Box3 Translate(Vector3 offset)
    {
        return new(LeftBottomBack + offset, RightTopFront + offset);
    }

    public readonly Box3 Union(Box3 other)
    {
        return new Box3(
            MathF.Min(Left, other.Left),
            MathF.Min(Bottom, other.Bottom),
            MathF.Min(Back, other.Back),
            MathF.Min(Right, other.Right),
            MathF.Min(Top, other.Top),
            MathF.Min(Front, other.Front)
        );
    }

    public Box3(Vector3 leftBottomBack, Vector3 rightTopFront)
    {
        Left = leftBottomBack.X;
        Bottom = leftBottomBack.Y;
        Back = leftBottomBack.Z;
        Right = rightTopFront.X;
        Top = rightTopFront.Y;
        Front = rightTopFront.Z;
    }

    public Box3(float left, float bottom, float back, float right, float top, float front)
    {
        Left = left;
        Bottom = bottom;
        Back = back;
        Right = right;
        Top = top;
        Front = front;
    }

    public Box3(float value)
    {
        Left = -value;
        Bottom = -value;
        Back = -value;
        Right = value;
        Top = value;
        Front = value;
    }

    public readonly bool Equals(Box3 other)
    {
        return Left == other.Left &&
            Bottom == other.Bottom &&
            Back == other.Back &&
            Right == other.Right &&
            Top == other.Top &&
            Front == other.Front;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Box3 other && Equals(other);
    }

    public static bool operator ==(Box3 a, Box3 b) => a.EqualsApprox(b);

    public static bool operator !=(Box3 a, Box3 b) => !a.EqualsApprox(b);

    public override readonly int GetHashCode() => HashCode.Combine(Left, Bottom, Back, Right, Top, Front);

    public override readonly string ToString()
    {
        return $"({Left}, {Bottom}, {Back}, {Right}, {Top}, {Front})";
    }
}
