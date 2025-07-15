namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiVisionVisualsRect : SharedStationAiVisionVisualsShape
{
    [DataField(required: true)]
    public Box2 Rect;

    [DataField(required: true)]
    public Color Color;
}
