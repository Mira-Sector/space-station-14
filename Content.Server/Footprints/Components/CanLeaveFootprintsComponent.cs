using Robust.Shared.Map;

namespace Content.Server.Footprints.Components;

[RegisterComponent]
public sealed partial class CanLeaveFootprintsComponent : Component
{
    /// <summary>
    /// Where the last footprint was.
    /// </summary>
    [ViewVariables]
    public MapCoordinates LastFootstep;

    /// <summary>
    /// How many footprints left to leave behind the entity.
    /// </summary>
    [ViewVariables]
    public uint FootstepsLeft = 1;

    [ViewVariables]
    public Color Color;
}
