using Content.Shared.Silicons.StationAi;

namespace Content.Client.Silicons.StationAi;

[RegisterComponent]
public sealed partial class StationAiVisionVisualsComponent : Component, IStationAiVisionVisuals
{
    [DataField]
    public SharedStationAiVisionVisualsShape[] Shapes { get; set; }

    [DataField]
    public bool BlockTiles;

    [DataField]
    public Dictionary<Enum, Dictionary<object, StationAiVisionVisualsAppearanceEntry>> AppearanceData = [];
}

[DataDefinition]
public sealed partial class StationAiVisionVisualsAppearanceEntry : IStationAiVisionVisuals
{
    [DataField]
    public SharedStationAiVisionVisualsShape[] Shapes { get; set; }
}
