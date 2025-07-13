using System.Numerics;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiVisionVisualsVertex : SharedStationAiVisionVisualsShape
{
    [DataField]
    public Vector2 Start;

    [DataField]
    public Vector2 End;

    [DataField]
    public Color Color;
}
