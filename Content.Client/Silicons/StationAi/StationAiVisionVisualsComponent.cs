using Content.Shared.Silicons.StationAi;

namespace Content.Client.Silicons.StationAi;

[RegisterComponent]
public sealed partial class StationAiVisionVisualsComponent : Component, IStationAiVisionVisuals
{
    [DataField]
    public SharedStationAiVisionVisualsShape[] Shapes { get; set; } = [];

    [DataField]
    public bool BlockTiles;

    [DataField]
    public bool NoRotation;

    [DataField]
    public bool SnapCardinals;

    [DataField]
    public Dictionary<Enum, Dictionary<string, StationAiVisionVisualsAppearanceEntry>> AppearanceData = [];
}

[DataDefinition]
public sealed partial class StationAiVisionVisualsAppearanceEntry : IStationAiVisionVisuals
{
    [DataField]
    public SharedStationAiVisionVisualsShape[] Shapes { get; set; }
}
