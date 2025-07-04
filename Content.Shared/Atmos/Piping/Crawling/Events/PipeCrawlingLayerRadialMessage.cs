using Content.Shared.Atmos.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Crawling.Events;

[Serializable, NetSerializable]
public sealed class PipeCrawlingLayerRadialMessage : BoundUserInterfaceMessage
{
    public readonly AtmosPipeLayer Layer;

    public PipeCrawlingLayerRadialMessage(AtmosPipeLayer layer)
    {
        Layer = layer;
    }
}
