using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Arcade.Racer.CollisionShapes;

public static class RacerArcadeObjectCollisionResolver
{
    public static bool Resolve(BaseRacerArcadeObjectCollisionShape a, BaseRacerArcadeObjectCollisionShape b, [NotNullWhen(true)] out Vector3? normal, [NotNullWhen(true)] out float? penetration)
    {
        // this massively reduces the amount of cases
        if (a.Complexity >= b.Complexity)
            return Handle(a, b, out normal, out penetration);

        var result = Handle(b, a, out normal, out penetration);
        if (result)
            normal = -normal; // flip normal as input isnt aware of the complexity ordering

        return result;
    }

    private static bool Handle(BaseRacerArcadeObjectCollisionShape main, BaseRacerArcadeObjectCollisionShape other, [NotNullWhen(true)] out Vector3? normal, [NotNullWhen(true)] out float? penetration)
    {
        return (main, other) switch
        {
            (Sphere mainSphere, Sphere otherSphere) => SphereVsSphere(mainSphere, otherSphere, out normal, out penetration),
            (Box mainBox, Box otherBox) => BoxVsBox(mainBox, otherBox, out normal, out penetration),
            (Box mainBox, Sphere otherSphere) => BoxVsSphere(mainBox, otherSphere, out normal, out penetration),
            _ => throw new NotImplementedException(),
        };
    }

    private static bool SphereVsSphere(Sphere main, Sphere other, [NotNullWhen(true)] out Vector3? normal, [NotNullWhen(true)] out float? penetration)
    {
        var mainCenter = main.Offset + main.Origin;
        var otherCenter = other.Offset + other.Origin;

        var delta = mainCenter - otherCenter;
        var distSq = delta.LengthSquared;

        var radiusSum = main.Radius + other.Radius;
        var radiusSumSquared = radiusSum * radiusSum;

        if (distSq > radiusSumSquared)
        {
            normal = null;
            penetration = null;
            return false;
        }

        var dist = MathF.Sqrt(distSq);

        normal = MathHelper.CloseTo(dist, 0f) ? Vector3.UnitZ : delta / dist;
        penetration = radiusSum - dist;
        return true;
    }

    private static bool BoxVsBox(Box main, Box other, [NotNullWhen(true)] out Vector3? normal, [NotNullWhen(true)] out float? penetration)
    {
        normal = null;
        penetration = null;

        var mainRot = main.GetBox();
        var otherRot = other.GetBox();

        var mainCenter = mainRot.Origin;
        var otherCenter = otherRot.Origin;

        // fast path
        // no rotation
        if (mainRot.Quaternion == Quaternion.Identity && otherRot.Quaternion == Quaternion.Identity)
        {
            if (!mainRot.Box.Intersects(otherRot.Box))
                return false;

            var dx = MathF.Min(mainRot.Box.Right - otherRot.Box.Left, otherRot.Box.Right - mainRot.Box.Left);
            var dy = MathF.Min(mainRot.Box.Top - otherRot.Box.Bottom, otherRot.Box.Top - mainRot.Box.Bottom);
            var dz = MathF.Min(mainRot.Box.Front - otherRot.Box.Back, otherRot.Box.Front - mainRot.Box.Back);

            penetration = MathF.Min(dx, MathF.Min(dy, dz));

            if (MathHelper.CloseTo(penetration.Value, dx))
                normal = new Vector3(MathF.Sign(mainCenter.X - otherCenter.X), 0, 0);
            else if (MathHelper.CloseTo(penetration.Value, dy))
                normal = new Vector3(0, MathF.Sign(mainCenter.Y - otherCenter.Y), 0);
            else
                normal = new Vector3(0, 0, MathF.Sign(mainCenter.Z - otherCenter.Z));

            return true;
        }

        // separating axis theorum

        var mainAxes = mainRot.GetLocalAxes();
        var otherAxes = otherRot.GetLocalAxes();

        var mainHalf = mainRot.Box.Size * 0.5f;
        var otherHalf = otherRot.Box.Size * 0.5f;

        var t = otherCenter - mainCenter;

        var bestAxis = Vector3.Zero;
        var minOverlap = float.MaxValue;

        // main axes
        foreach (var mainAxis in mainAxes)
        {
            if (!OverlapOnAxis(t, mainAxis, mainAxes, mainHalf, otherAxes, otherHalf, out var overlap))
                return false;

            UpdateOverlap(overlap, mainAxis);
        }

        // other axes
        foreach (var otherAxis in otherAxes)
        {
            if (!OverlapOnAxis(t, otherAxis, mainAxes, mainHalf, otherAxes, otherHalf, out var overlap))
                return false;

            UpdateOverlap(overlap, otherAxis);
        }

        // cross product axes
        foreach (var mainAxis in mainAxes)
        {
            foreach (var otherAxis in otherAxes)
            {
                var axis = Vector3.Cross(mainAxis, otherAxis);

                // check parallel lines
                if (MathHelper.CloseTo(axis.LengthSquared, 0f))
                    continue;

                if (!OverlapOnAxis(t, axis, mainAxes, mainHalf, otherAxes, otherHalf, out var overlap))
                    return false;

                UpdateOverlap(overlap, axis);
            }
        }

        normal = Vector3.Normalize(bestAxis);
        if (Vector3.Dot(normal.Value, t) < 0f)
            normal = -normal;

        penetration = minOverlap;
        return true;

        static bool OverlapOnAxis(
            Vector3 t,
            Vector3 axis,
            Vector3[] axesA,
            Vector3 halfA,
            Vector3[] axesB,
            Vector3 halfB,
            out float overlap)
        {
            axis = Vector3.Normalize(axis);
            var projection = Math.Abs(Vector3.Dot(t, axis));

            var radius = 0f;
            radius += Math.Abs(Vector3.Dot(axesA[0], axis)) * halfA.X;
            radius += Math.Abs(Vector3.Dot(axesA[1], axis)) * halfA.Y;
            radius += Math.Abs(Vector3.Dot(axesA[2], axis)) * halfA.Z;

            radius += Math.Abs(Vector3.Dot(axesB[0], axis)) * halfB.X;
            radius += Math.Abs(Vector3.Dot(axesB[1], axis)) * halfB.Y;
            radius += Math.Abs(Vector3.Dot(axesB[2], axis)) * halfB.Z;


            overlap = radius - projection;
            return overlap >= 0f;
        }

        void UpdateOverlap(float overlap, Vector3 axis)
        {
            axis = Vector3.Normalize(axis);

            if (overlap < minOverlap)
            {
                minOverlap = overlap;
                bestAxis = axis;
            }
        }
    }

    private static bool BoxVsSphere(Box main, Sphere other, [NotNullWhen(true)] out Vector3? normal, [NotNullWhen(true)] out float? penetration)
    {
        var invRot = Quaternion.Invert(main.Rotation);

        var otherCenterWorld = other.Offset + other.Origin;
        var otherCenterLocal = Vector3.Transform(otherCenterWorld - main.Center, invRot);

        var otherClampedLocal = Vector3.Clamp(otherCenterLocal, main.Box3.LeftBottomBack, main.Box3.RightTopFront);

        var deltaLocal = otherCenterLocal - otherClampedLocal;
        var distSq = deltaLocal.LengthSquared;

        var otherRadiusSquared = other.Radius * other.Radius;

        if (distSq > otherRadiusSquared)
        {
            normal = null;
            penetration = null;
            return false;
        }

        var dist = MathF.Sqrt(distSq);

        var localNormal = MathHelper.CloseTo(dist, 0f) ? Vector3.UnitZ : deltaLocal / dist;
        normal = Vector3.Normalize(Vector3.Transform(localNormal, main.Rotation));
        penetration = other.Radius - dist;
        return true;
    }
}
