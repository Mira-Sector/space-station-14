using Robust.Shared.Serialization;

namespace Content.Shared.PolygonRenderer;

[DataDefinition, Virtual, Serializable, NetSerializable]
public partial class PolygonModel
{
    [DataField]
    public List<BasePolygon> Polygons;

    [ViewVariables]
    public Matrix4 ModelMatrix = Matrix4.Identity;

    public PolygonModel(List<BasePolygon> polygons)
    {
        Polygons = polygons;
    }

    public PolygonModel(List<BasePolygon> polygons, Matrix4 modelMatrix)
    {
        Polygons = polygons;
        ModelMatrix = modelMatrix;
    }
}
