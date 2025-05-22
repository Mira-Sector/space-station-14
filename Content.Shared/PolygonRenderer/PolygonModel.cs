using Robust.Shared.Serialization;

namespace Content.Shared.PolygonRenderer;

[DataDefinition, Virtual, Serializable, NetSerializable]
public partial class PolygonModel
{
    [DataField]
    public List<Polygon> Polygons;

    [DataField]
    public Vector3 Position;

    [DataField]
    public Vector3 Rotation;

    [DataField]
    public Vector3 Scale = Vector3.One;

    public PolygonModel(List<Polygon> polygons)
    {
        Polygons = polygons;
    }

    public PolygonModel(List<Polygon> polygons, Vector3 position)
    {
        Polygons = polygons;
        Position = position;
    }

    public PolygonModel(List<Polygon> polygons, Vector3 position, Vector3 rotation)
    {
        Polygons = polygons;
        Position = position;
        Rotation = rotation;
    }

    public PolygonModel(List<Polygon> polygons, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        Polygons = polygons;
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }
}
