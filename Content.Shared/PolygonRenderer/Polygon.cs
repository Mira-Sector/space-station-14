using Robust.Shared.Serialization;

namespace Content.Shared.PolygonRenderer;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class Polygon
{
    [DataField]
    public Vector3[] Vertices = new Vector3[3];

    [DataField]
    public Color? Color;

    public Vector3 Centroid => new()
    {
        X = (Vertices[0].X + Vertices[1].X + Vertices[2].X) / 3f,
        Y = (Vertices[0].Y + Vertices[1].Y + Vertices[2].Y) / 3f,
        Z = (Vertices[0].Z + Vertices[1].Z + Vertices[2].Z) / 3f
    };

    private Vector3? _normal;

    public Vector3 Normal => _normal ??= CalculateNormal();

    internal Vector3 CalculateNormal()
    {
        var edge1 = Vertices[1] - Vertices[0];
        var edge2 = Vertices[2] - Vertices[0];
        var normal = Vector3.Cross(edge1, edge2);
        return Vector3.Normalize(normal);
    }

    public Polygon(Vector3[] vertices)
    {
        ValidateVertices(vertices);
        Vertices = vertices;
    }

    public Polygon(Vector3[] vertices, Color? color)
    {
        ValidateVertices(vertices);
        Vertices = vertices;
        Color = color;
    }

    internal static void ValidateVertices(Vector3[] vertices)
    {
        if (vertices.Length != 3)
            throw new ArgumentException("Vertices array must contain exactly 3 elements", nameof(vertices));
    }
}
