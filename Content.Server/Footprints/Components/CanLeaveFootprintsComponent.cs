using Content.Shared.Chemistry.Components;
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
    /// The last puddle the player was in.
    /// Used to check if we need to recalculate the taking of liquid
    /// </summary>
    [ViewVariables]
    public EntityUid LastPuddle;

    /// <summary>
    /// How many footprints left to leave behind the entity.
    /// </summary>
    [ViewVariables]
    public uint FootstepsLeft = 1;

    /// <summary>
    /// If non null represets if the decal is either the alt or normal decal.
    /// Null represents always use normal.
    /// </summary>
    [ViewVariables]
    public bool? UseAlternative;

    [ViewVariables]
    public Entity<SolutionComponent> Solution;

    [ViewVariables]
    public string? Container;

    [ViewVariables]
    public float Alpha = 1f;
}
