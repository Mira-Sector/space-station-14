namespace Content.Shared.Surgery;

[DataDefinition]
public class SurgeryEdge
{
    /// <summary>
    /// Requirements that must be met for this edge to be taken.
    /// </summary>
    [DataField]
    public SurgeryEdgeRequirement[] Requirements { get; set; } = Array.Empty<SurgeryEdgeRequirement>();

    /// <summary>
    /// What node does this edge connect to.
    /// </summary>
    /// <remarks>
    /// null represents no connection.
    /// </remarks>
    [DataField]
    public string? Connection;
}
