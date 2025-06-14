using Content.Shared.Atmos.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Crawling.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PipeCrawlingPipeComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Dictionary<AtmosPipeLayer, Dictionary<Direction, NetEntity>> ConnectedPipes = [];
}
