namespace Content.Server.Forensics;

/// <summary>
/// This controls residues left on items
/// which the forensics system uses.
/// </summary>
[RegisterComponent]
public sealed partial class ResidueComponent : Component
{
    [DataField]
    public LocId ResidueAdjective = "residue-unknown";

    [DataField]
    public Color? ResidueColor;

    [DataField]
    public List<ResidueAge> ResidueAge = new();
}

[DataDefinition]
public partial struct ResidueAge
{
    [DataField]
    public LocId AgeLocId {get; set;}

    [DataField]
    public uint AgeThrestholdMin {get; set;}
}
