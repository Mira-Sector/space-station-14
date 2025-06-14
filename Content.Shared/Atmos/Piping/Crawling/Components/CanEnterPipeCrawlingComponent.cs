using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Crawling.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CanEnterPipeCrawlingComponent : Component
{
    [DataField]
    public TimeSpan MoveDelay = TimeSpan.FromSeconds(0.25);
}
