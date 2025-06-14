using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Crawling.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PipeCrawlingAutoPilotComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid TargetPipe;
}
