using Robust.Shared.Serialization;

namespace Content.Shared.Surgery;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class SurgeryNode
{
    [DataField]
    public string? Id { get; protected set; } = default!;

    /// <summary>
    /// Edges this node has to other nodes.
    /// </summary>
    /// <remarks>
    /// This edge cannot always be taken as it may have requirements <see cref=SurgeryEdge.Requirements>.
    /// </remarks>
    [DataField]
    public List<SurgeryEdge> Edges { get; set; } = new();

    /// <summary>
    /// List of special actions such as adding components which get run whenever this node is reached and left.
    /// </summary>
    [DataField]
    public HashSet<SurgerySpecial> Special { get; set; } = new();
}
