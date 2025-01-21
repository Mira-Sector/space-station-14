namespace Content.Shared.Surgery;

[DataDefinition, Serializable]
public partial class SurgeryNode
{
    [DataField]
    public string? ID { get; protected set; } = default!;

    /// <summary>
    /// Edges this node has to other nodes.
    /// </summary>
    /// <remarks>
    /// This edge cannot always be taken as it may have requirements <see cref=SurgeryEdge.Requirements>.
    /// </remarks>
    [DataField]
    public HashSet<SurgeryEdge> Edges { get; set; } = new();

    /// <summary>
    /// List of special actions such as adding components which get run whenever this node is reached and left.
    /// </summary>
    [DataField]
    public SurgerySpecial[] Special { get; set; } = Array.Empty<SurgerySpecial>();

    /// <summary>
    /// Edges connected to this node.
    /// </summary>
    [ViewVariables]
    public HashSet<SurgeryEdge> Connections = new();
}
