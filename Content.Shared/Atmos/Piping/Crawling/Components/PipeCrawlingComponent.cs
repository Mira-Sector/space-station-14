using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Atmos.Piping.Crawling.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PipeCrawlingComponent : Component
{
    [ViewVariables]
    public EntityUid CurrentPipe;

    [ViewVariables]
    public Dictionary<string, bool> OriginalCollision = new();

    [ViewVariables]
    public (EntityCoordinates, Direction) LastPos;
}
