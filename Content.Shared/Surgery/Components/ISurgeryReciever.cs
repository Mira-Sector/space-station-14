using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Components;

public interface ISurgeryReciever
{
    /// <summary>
    /// List of surgery graphs that will get merged into one <see cref=SurgeryRecieverComponent.Graph>
    /// </summary>
    [DataField]
    public List<ProtoId<SurgeryPrototype>> AvailableSurgeries { get; set; }

    /// <summary>
    /// All the surgeries graphs merged into one graph.
    /// </summary>
    [ViewVariables]
    public SurgeryGraph Graph { get; set; }

    [ViewVariables]
    public SurgeryNode? CurrentNode { get; set; }

    /// <summary>
    /// Keep track of doafters as they will need to be cancelled when we change node
    /// </summary>
    [ViewVariables]
    public HashSet<DoAfterId> DoAfters { get; set; }
}
