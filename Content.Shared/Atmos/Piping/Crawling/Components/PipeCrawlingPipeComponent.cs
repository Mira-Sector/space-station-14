using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Crawling.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PipeCrawlingPipeComponent : Component
{
    [ViewVariables]
    public bool Enabled = false;

    [ViewVariables]
    public List<EntityUid> ContainedEntities = new();

    [ViewVariables]
    public PipeDirection ConnectedPipeDir;

    [ViewVariables]
    public PipeDirection OpenPipeDir;
}
