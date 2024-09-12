namespace Content.Server.Footprints.Components;

[RegisterComponent]
public sealed partial class LeavesFootprintsComponent : Component
{
    /// <summary>
    /// How many footsteps to leave behind the player once they step on something which gives it
    /// </summary>
    [DataField]
    public uint MaxFootsteps = 3;

    /// <summary>
    /// How far should the player have to walk until we leave a footprint
    /// </summary>
    [DataField]
    public float Distance = 1.5f;

    /// <summary>
    /// What decal to leave behind when the entity moves.
    /// </summary>
    [DataField]
    public string FootprintDecal = "Box";
}
