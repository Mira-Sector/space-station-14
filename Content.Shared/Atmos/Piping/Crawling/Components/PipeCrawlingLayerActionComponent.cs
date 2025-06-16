using Content.Shared.Atmos.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Atmos.Piping.Crawling.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PipeCrawlingLayerActionComponent : Component
{
    [DataField]
    public Dictionary<AtmosPipeLayer, SpriteSpecifier> IconSprites = [];
}
