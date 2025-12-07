using Robust.Shared.Serialization;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Shared.PolygonRenderer;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class BasePolygon
{
    [DataField]
    public Vector3[] Vertices = new Vector3[3];

    public Vector3 Centroid => new(
        (Vertices[0].X + Vertices[1].X + Vertices[2].X) / 3f,
        (Vertices[0].Y + Vertices[1].Y + Vertices[2].Y) / 3f,
        (Vertices[0].Z + Vertices[1].Z + Vertices[2].Z) / 3f
    );

    public Vector3 Normal()
    {
        var edge1 = Vertices[1] - Vertices[0];
        var edge2 = Vertices[2] - Vertices[0];
        var normal = Vector3.Cross(edge1, edge2);

        if (normal.LengthSquared < float.Epsilon)
            return Vector3.Zero;

        return Vector3.Normalize(normal);
    }

    public abstract Color? Shade(Vector3[] cameraVertices, Matrix4 camera);

    public BasePolygon(Vector3[] vertices)
    {
        ValidateVertices(vertices);
        Vertices = vertices;
    }

    private static void ValidateVertices(Vector3[] vertices)
    {
        if (vertices.Length != 3)
            throw new ArgumentException("Vertices array must contain exactly 3 elements", nameof(vertices));
    }
}
