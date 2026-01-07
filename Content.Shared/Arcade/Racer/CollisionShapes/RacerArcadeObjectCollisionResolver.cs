using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Arcade.Racer.CollisionShapes;

public static class RacerArcadeObjectCollisionResolver
{
    public static bool Resolve(
        BaseRacerArcadeObjectCollisionShape a,
        BaseRacerArcadeObjectCollisionShape b,
        (Vector3 Position, Quaternion Rotation) aOffset,
        (Vector3 Position, Quaternion Rotation) bOffset,
        [NotNullWhen(true)] out Vector3? normal,
        [NotNullWhen(true)] out float? penetration)
    {
        // this massively reduces the amount of cases
        if (a.Complexity >= b.Complexity)
            return Handle(a, b, aOffset, bOffset, out normal, out penetration);

        var result = Handle(b, a, bOffset, aOffset, out normal, out penetration);
        if (result)
            normal = -normal; // flip normal as input isnt aware of the complexity ordering

        return result;
    }

    private static bool Handle(
        BaseRacerArcadeObjectCollisionShape a,
        BaseRacerArcadeObjectCollisionShape b,
        (Vector3 Position, Quaternion Rotation) aOffset,
        (Vector3 Position, Quaternion Rotation) bOffset,
        [NotNullWhen(true)] out Vector3? normal,
        [NotNullWhen(true)] out float? penetration)
    {
        return (a, b) switch
        {
            (Sphere aSphere, Sphere bSphere) => SphereVsSphere(aSphere, bSphere, aOffset, bOffset, out normal, out penetration),
            (Box aBox, Box bBox) => BoxVsBox(aBox, bBox, aOffset, bOffset, out normal, out penetration),
            (Box aBox, Sphere bSphere) => BoxVsSphere(aBox, bSphere, aOffset, bOffset, out normal, out penetration),
            _ => throw new NotImplementedException(),
        };
    }

    private static bool SphereVsSphere(
        Sphere a,
        Sphere b,
        (Vector3 Position, Quaternion Rotation) aOffset,
        (Vector3 Position, Quaternion Rotation) bOffset,
        [NotNullWhen(true)] out Vector3? normal,
        [NotNullWhen(true)] out float? penetration)
    {
        var aLocal = a.Origin + a.Offset;
        var bLocal = b.Origin + b.Offset;

        var aCenter = aOffset.Position + Vector3.Transform(aLocal, aOffset.Rotation);
        var bCenter = bOffset.Position + Vector3.Transform(bLocal, bOffset.Rotation);

        var delta = aCenter - bCenter;
        var distSq = delta.LengthSquared;

        var radiusSum = a.Radius + b.Radius;
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

    private static bool BoxVsBox(
        Box a,
        Box b,
        (Vector3 Position, Quaternion Rotation) aOffset,
        (Vector3 Position, Quaternion Rotation) bOffset,
        [NotNullWhen(true)] out Vector3? normal,
        [NotNullWhen(true)] out float? penetration)
    {
        normal = null;
        penetration = null;

        var aRot = a.GetBox();
        aRot = aRot.Translate(aOffset.Position);
        aRot = aRot.Rotate(aOffset.Rotation);

        var bRot = b.GetBox();
        bRot = bRot.Translate(bOffset.Position);
        bRot = bRot.Rotate(bOffset.Rotation);

        var aCenter = aRot.Origin;
        var bCenter = bRot.Origin;

        // fast path
        // no rotation
        if (aRot.Quaternion == Quaternion.Identity && bRot.Quaternion == Quaternion.Identity)
        {
            if (!aRot.Box.Intersects(bRot.Box))
                return false;

            var dx = MathF.Min(aRot.Box.Right - bRot.Box.Left, bRot.Box.Right - aRot.Box.Left);
            var dy = MathF.Min(aRot.Box.Top - bRot.Box.Bottom, bRot.Box.Top - aRot.Box.Bottom);
            var dz = MathF.Min(aRot.Box.Front - bRot.Box.Back, bRot.Box.Front - aRot.Box.Back);

            penetration = MathF.Min(dx, MathF.Min(dy, dz));

            if (MathHelper.CloseTo(penetration.Value, dx))
                normal = new Vector3(MathF.Sign(aCenter.X - bCenter.X), 0, 0);
            else if (MathHelper.CloseTo(penetration.Value, dy))
                normal = new Vector3(0, MathF.Sign(aCenter.Y - bCenter.Y), 0);
            else
                normal = new Vector3(0, 0, MathF.Sign(aCenter.Z - bCenter.Z));

            return true;
        }

        // separating axis theorum

        var aAxes = aRot.GetLocalAxes();
        var bAxes = bRot.GetLocalAxes();

        var aHalf = aRot.Box.Size * 0.5f;
        var bHalf = bRot.Box.Size * 0.5f;

        var t = bCenter - aCenter;

        var bestAxis = Vector3.Zero;
        var minOverlap = float.MaxValue;

        // a axes
        foreach (var aAxis in aAxes)
        {
            if (!OverlapOnAxis(t, aAxis, aAxes, aHalf, bAxes, bHalf, out var overlap))
                return false;

            UpdateOverlap(overlap, aAxis);
        }

        // b axes
        foreach (var bAxis in bAxes)
        {
            if (!OverlapOnAxis(t, bAxis, aAxes, aHalf, bAxes, bHalf, out var overlap))
                return false;

            UpdateOverlap(overlap, bAxis);
        }

        // cross product axes
        foreach (var aAxis in aAxes)
        {
            foreach (var bAxis in bAxes)
            {
                var axis = Vector3.Cross(aAxis, bAxis);

                // check parallel lines
                if (MathHelper.CloseTo(axis.LengthSquared, 0f))
                    continue;

                if (!OverlapOnAxis(t, axis, aAxes, aHalf, bAxes, bHalf, out var overlap))
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

    private static bool BoxVsSphere(
        Box a,
        Sphere b,
        (Vector3 Position, Quaternion Rotation) aOffset,
        (Vector3 Position, Quaternion Rotation) bOffset,
        [NotNullWhen(true)] out Vector3? normal,
        [NotNullWhen(true)] out float? penetration)
    {
        var invRot = Quaternion.Invert(a.Rotation);

        var bCenterWorld = bOffset.Position + Vector3.Transform(b.Offset + b.Origin, bOffset.Rotation);
        var bCenterLocal = Vector3.Transform(bCenterWorld - aOffset.Position, invRot) - a.Origin;

        var bClampedLocal = Vector3.Clamp(bCenterLocal, a.Box3.LeftBottomBack, a.Box3.RightTopFront);

        var deltaLocal = bCenterLocal - bClampedLocal;
        var distSq = deltaLocal.LengthSquared;

        var bRadiusSquared = b.Radius * b.Radius;

        if (distSq > bRadiusSquared)
        {
            normal = null;
            penetration = null;
            return false;
        }

        var dist = MathF.Sqrt(distSq);

        var localNormal = MathHelper.CloseTo(dist, 0f) ? Vector3.UnitZ : deltaLocal / dist;
        normal = Vector3.Normalize(Vector3.Transform(localNormal, aOffset.Rotation));
        penetration = b.Radius - dist;
        return true;
    }
}
