using System.Numerics;
using Robust.Shared.Serialization;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Shared.PolygonRenderer;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class Polygon
{
    [DataField]
    public Vector3[] Vertices = new Vector3[3];

    public Vector3 Centroid => new()
    {
        X = (Vertices[0].X + Vertices[1].X + Vertices[2].X) / 3f,
        Y = (Vertices[0].Y + Vertices[1].Y + Vertices[2].Y) / 3f,
        Z = (Vertices[0].Z + Vertices[1].Z + Vertices[2].Z) / 3f
    };

    public Vector3 Normal()
    {
        var edge1 = Vertices[1] - Vertices[0];
        var edge2 = Vertices[2] - Vertices[0];
        var normal = Vector3.Cross(edge1, edge2);

        if (normal.LengthSquared < float.Epsilon)
            return Vector3.Zero;

        return Vector3.Normalize(normal);
    }

    public virtual (List<Vector2>, Color?) PolygonTo2D(Vector3 camera)
    {
        List<Vector2> projectedPoints = [];

        foreach (var vertex in Vertices)
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

        return (projectedPoints, Color.White);
    }

    public Polygon(Vector3[] vertices)
    {
        ValidateVertices(vertices);
        Vertices = vertices;
    }

    internal static void ValidateVertices(Vector3[] vertices)
    {
        if (vertices.Length != 3)
            throw new ArgumentException("Vertices array must contain exactly 3 elements", nameof(vertices));
    }
}
