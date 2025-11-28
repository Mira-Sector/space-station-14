namespace Content.Shared.Arcade.Racer.PhysShapes;

public static class RacerArcadeObjectPhysCollisionResolver
{
    public static bool Resolve(BaseRacerArcadeObjectPhysShape a, BaseRacerArcadeObjectPhysShape b)
    {
        // this massively reduces the amount of cases
        if (a.Complexity >= b.Complexity)
            return Handle(a, b);

        return Handle(b, a);
    }

    private static bool Handle(BaseRacerArcadeObjectPhysShape main, BaseRacerArcadeObjectPhysShape other)
    {
        return (main, other) switch
        {
            (Sphere mainSphere, Sphere otherSphere) => SphereVsSphere(mainSphere, otherSphere),
            (Box mainBox, Box otherBox) => BoxVsBox(mainBox, otherBox),
            (Box mainBox, Sphere otherSphere) => BoxVsSphere(mainBox, otherSphere),
            _ => throw new NotImplementedException(),
        };
    }

    private static bool SphereVsSphere(Sphere main, Sphere other)
    {
        var mainCenter = main.Offset + main.Origin;
        var otherCenter = other.Offset + other.Origin;

        var radiusSum = main.Radius + other.Radius;
        var radiusSumSquared = radiusSum * radiusSum;

        return (mainCenter - otherCenter).LengthSquared <= radiusSumSquared;
    }

    private static bool BoxVsBox(Box main, Box other)
    {
        var mainRot = main.GetBox();
        var otherRot = other.GetBox();

        // fast path
        // no rotation
        if (mainRot.Quaternion == Quaternion.Identity && otherRot.Quaternion == Quaternion.Identity)
            return mainRot.Box.Intersects(otherRot.Box);

        // separating axis theorum

        var mainAxes = mainRot.GetLocalAxes();
        var otherAxes = otherRot.GetLocalAxes();

        var mainCenter = mainRot.Origin;
        var otherCenter = otherRot.Origin;
        var mainHalf = mainRot.Box.Size * 0.5f;
        var otherHalf = otherRot.Box.Size * 0.5f;

        var t = otherCenter - mainCenter;

        // main axes
        foreach (var mainAxis in mainAxes)
        {
            if (!OverlapOnAxis(t, mainAxis, mainAxes, mainHalf, otherAxes, otherHalf))
                return false;
        }

        // other axes
        foreach (var otherAxis in otherAxes)
        {
            if (!OverlapOnAxis(t, otherAxis, mainAxes, mainHalf, otherAxes, otherHalf))
                return false;
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

                if (!OverlapOnAxis(t, axis, mainAxes, mainHalf, otherAxes, otherHalf))
                    return false;
            }
        }

        return true;
    }

    private static bool BoxVsSphere(Box main, Sphere other)
    {
        var invRot = Quaternion.Invert(main.Rotation);

        var otherCenterLocal = Vector3.Transform(other.Offset + other.Origin - main.Center + main.Origin, invRot);
        var closestLocal = Vector3.Clamp(otherCenterLocal, main.Box3.LeftBottomBack + main.Origin, main.Box3.RightTopFront + main.Origin);

        var otherRadiusSquared = other.Radius * other.Radius;
        return (closestLocal - otherCenterLocal).LengthSquared <= otherRadiusSquared;
    }

    private static bool OverlapOnAxis(
        Vector3 t,
        Vector3 axis,
        Vector3[] axesA,
        Vector3 halfA,
        Vector3[] axesB,
        Vector3 halfB)
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
        return projection <= radius;
    }
}
