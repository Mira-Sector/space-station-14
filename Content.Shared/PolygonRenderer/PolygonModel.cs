using Robust.Shared.Serialization;

namespace Content.Shared.PolygonRenderer;

[DataDefinition, Virtual, Serializable, NetSerializable]
public partial class PolygonModel
{
    [DataField]
    public List<Polygon> Polygons;

    public PolygonModel(List<Polygon> polygons)
    {
        Polygons = polygons;
    }
}
