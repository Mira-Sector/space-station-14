using System.Numerics;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiVisionVisualsVertex : SharedStationAiVisionVisualsShape
{
    [DataField(required: true)]
    public Vector2 Start;

    [DataField(required: true)]
    public Vector2 End;

    [DataField(required: true)]
    public Color Color;
}
