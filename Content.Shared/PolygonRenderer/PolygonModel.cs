using Robust.Shared.Serialization;

namespace Content.Shared.PolygonRenderer;

[DataDefinition, Virtual, Serializable, NetSerializable]
public partial class PolygonModel
{
    [DataField]
    public List<Polygon> Polygons;

    [DataField]
    public Matrix4 ModelMatrix = Matrix4.Identity;

    public PolygonModel(List<Polygon> polygons)
    {
        Polygons = polygons;
    }

    public PolygonModel(List<Polygon> polygons, Matrix4 modelMatrix)
    {
        Polygons = polygons;
        ModelMatrix = modelMatrix;
    }
}
