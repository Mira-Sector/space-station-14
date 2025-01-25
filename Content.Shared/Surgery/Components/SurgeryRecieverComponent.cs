using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryRecieverComponent : Component
{
    /// <summary>
    /// List of surgery graphs that will get merged into one <see cref=SurgeryRecieverComponent.Graph>
    /// </summary>
    [DataField]
    public List<ProtoId<SurgeryPrototype>> AvailableSurgeries = new();

    /// <summary>
    /// All the surgeries graphs merged into one graph.
    /// </summary>
    [ViewVariables]
    public SurgeryGraph Graph = new();

    [ViewVariables]
    public SurgeryNode? CurrentNode;

    /// <summary>
    /// Keep track of doafters as they will need to be cancelled when we change node
    /// </summary>
    [ViewVariables]
    public List<DoAfterId> DoAfters = new();
}
