using System.Numerics;
using Content.Shared.PolygonRenderer;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.PolygonRenderer;

public sealed class PolygonRenderer
{
    public static List<Vector2> PolygonTo2D(Polygon polygon, Vector3 camera)
    {
        var projectedPoints = new List<Vector2>(3);

        foreach (var vertex in polygon.Vertices)
        {
            var relativePos = vertex - camera;
            relativePos.Z += float.Epsilon; // prevent division by 0

            var projected = new Vector2()
            {
                X = relativePos.X / relativePos.Z,
                Y = relativePos.Y / relativePos.Z
            };

            projectedPoints.Add(projected);
        }

        return projectedPoints;
    }
}
