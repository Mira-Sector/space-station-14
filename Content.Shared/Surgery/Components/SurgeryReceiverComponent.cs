using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class SurgeryReceiverComponent : Component, ISurgeryReceiver
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<SurgeryPrototype>> AvailableSurgeries { get; set; } = new();

    [ViewVariables, AutoNetworkedField]
    public SurgeryGraph Graph { get; set; } = new();

    [ViewVariables, AutoNetworkedField]
    public SurgeryNode? CurrentNode { get; set; }

    [ViewVariables, AutoNetworkedField]
    public Dictionary<DoAfterId, (EntityUid, SurgeryEdgeRequirement)> DoAfters { get; set; } = new();

    [ViewVariables, AutoNetworkedField]
    public HashSet<Enum> UserInterfaces { get; set; } = new();
}
