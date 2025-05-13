using Robust.Shared.Serialization;

namespace Content.Shared.Surgery;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class SurgeryEdge
{
    /// <summary>
    /// Requirements that must be met for this edge to be taken.
    /// </summary>
    [DataField(required:true)]
    public SurgeryEdgeRequirement Requirement { get; set; } = default!;

    [DataField("connection")]
    public string? _connection { get; private set; }

    /// <summary>
    /// Hashcode of what node does this edge connect to.
    /// </summary>
    /// <remarks>
    /// null represents no connection.
    /// </remarks>
    [ViewVariables]
    public int? Connection;
}
