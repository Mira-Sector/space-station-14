namespace Content.Shared.Surgery;

[Virtual, DataDefinition]
public partial class SurgeryGraph
{
    /// <summary>
    /// List of nodes this graph contains
    /// </summary>
    [DataField]
    public List<SurgeryNode> Nodes { get; set; } = new();
}
