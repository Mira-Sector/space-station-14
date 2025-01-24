using Robust.Shared.Serialization;

namespace Content.Shared.Surgery;

[DataDefinition, Serializable, NetSerializable]
public partial class SurgeryEdge
{
    /// <summary>
    /// Requirements that must be met for this edge to be taken.
    /// </summary>
    [DataField]
    public SurgeryEdgeRequirement[] Requirements { get; set; } = Array.Empty<SurgeryEdgeRequirement>();

    [DataField("connection")]
    public string? _connection;

    /// <summary>
    /// What node does this edge connect to.
    /// </summary>
    /// <remarks>
    /// null represents no connection.
    /// </remarks>
    [ViewVariables]
    public SurgeryNode? Connection;
}
