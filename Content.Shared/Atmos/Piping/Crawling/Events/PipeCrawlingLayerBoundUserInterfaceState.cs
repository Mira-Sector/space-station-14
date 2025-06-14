using Content.Shared.Atmos.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Atmos.Piping.Crawling.Events;

[Serializable, NetSerializable]
public sealed partial class PipeCrawlingLayerBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly Dictionary<AtmosPipeLayer, SpriteSpecifier> Layers = [];

    public PipeCrawlingLayerBoundUserInterfaceState(Dictionary<AtmosPipeLayer, SpriteSpecifier> layers)
    {
        Layers = layers;
    }
}
