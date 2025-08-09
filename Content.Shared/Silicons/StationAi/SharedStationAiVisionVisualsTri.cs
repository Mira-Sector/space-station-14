using System.Numerics;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiVisionVisualsTri : SharedStationAiVisionVisualsShape
{
    [DataField(required: true)]
    public Vector2 Point1;

    [DataField(required: true)]
    public Vector2 Point2;

    [DataField(required: true)]
    public Vector2 Point3;

    public List<Vector2> Points => new()
    {
        Point1,
        Point2,
        Point3
    };

    [DataField(required: true)]
    public Color Color;
}
