using Robust.Shared.Prototypes;

namespace Content.Server.Footprints.Components;

[RegisterComponent]
public sealed partial class LeavesFootprintsComponent : Component
{
    /// <summary>
    /// How many footsteps to leave behind the player once they step on something which gives it
    /// </summary>
    [DataField]
    public uint MaxFootsteps = 8;

    /// <summary>
    /// How far should the player have to walk until we leave a footprint
    /// </summary>
    [DataField]
    public float Distance = 0.8f;

    /// <summary>
    /// What entity to leave behind when the entity moves.
    /// </summary>
    [DataField]
    public EntProtoId[] FootprintPrototypes =
    [
        new("FootprintFootLeft"),
        new("FootprintFootRight")
    ];
}
