namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiVisionVisualsRect : SharedStationAiVisionVisualsShape
{
    [DataField]
    public Box2 Rect;

    [DataField]
    public Color Color;
}
