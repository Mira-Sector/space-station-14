using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class StationAiVisionTileVisuals : IStationAiVisionVisuals
{
    [DataField]
    public SharedStationAiVisionVisualsShape[] Shapes { get; set; }
}
