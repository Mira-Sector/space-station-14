using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Crawling.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PipeCrawlingEnterPointComponent : Component
{
    [DataField]
    public bool CanEnter = true;

    [DataField]
    public bool CanExit = true;

    [ViewVariables]
    public bool Enterable = false;

    [ViewVariables]
    public bool Exitable = false;
}
