using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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
    [ViewVariables, AutoNetworkedField]
    public SurgeryGraph Graph = new();

    [ViewVariables]
    public SurgeryNode? CurrentNode;
}
