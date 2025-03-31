using Robust.Shared.Map;

namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterDelaminationTeleportedComponent: Component
{
    [ViewVariables]
    public MapCoordinates StartingCoords;
}
