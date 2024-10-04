using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Crawling.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PipeCrawlingPipeComponent : Component
{
    [ViewVariables]
    [AutoNetworkedField]
    public List<EntityUid> ContainedEntities = new();

    [ViewVariables]
    [AutoNetworkedField]
    public Dictionary<Direction, EntityUid> ConnectedPipes = new();

    [ViewVariables]
    public List<EntityUid> UpdatedBy = new();
}
