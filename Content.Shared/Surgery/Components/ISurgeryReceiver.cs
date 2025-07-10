using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Components;

public interface ISurgeryReceiver
{
    /// <summary>
    /// List of surgery graphs that will get merged into one <see cref=SurgeryReceiverComponent.Graph>
    /// </summary>
    [DataField]
    List<ProtoId<SurgeryPrototype>> AvailableSurgeries { get; set; }

    /// <summary>
    /// All the surgeries graphs merged into one graph.
    /// </summary>
    [ViewVariables]
    SurgeryGraph Graph { get; set; }

    [ViewVariables]
    SurgeryNode? CurrentNode { get; set; }

    /// <summary>
    /// Keep track of doafters as they will need to be cancelled when we change node
    /// </summary>
    [ViewVariables]
    Dictionary<(NetEntity, ushort), (NetEntity, SurgeryEdgeRequirement)> DoAfters { get; set; }

    /// <summary>
    /// Keep track of the open uis as they will need to be closed
    /// </summary>
    [ViewVariables]
    HashSet<Enum> UserInterfaces { get; set; }
}
