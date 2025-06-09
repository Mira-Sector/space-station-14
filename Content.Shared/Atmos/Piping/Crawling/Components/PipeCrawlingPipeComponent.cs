using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Crawling.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PipeCrawlingPipeComponent : Component
{
    [ViewVariables]
    public Dictionary<Direction, EntityUid> ConnectedPipes = new();

    [ViewVariables]
    public List<EntityUid> UpdatedBy = new();
}
