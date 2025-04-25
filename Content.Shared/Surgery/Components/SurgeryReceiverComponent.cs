using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryReceiverComponent : Component, ISurgeryReceiver
{
    [DataField]
    public List<ProtoId<SurgeryPrototype>> AvailableSurgeries { get; set; } = new();

    [ViewVariables]
    public SurgeryGraph Graph { get; set; } = new();

    [ViewVariables]
    public SurgeryNode? CurrentNode { get; set; }

    [ViewVariables]
    public Dictionary<DoAfterId, (EntityUid, SurgeryEdgeRequirement)> DoAfters { get; set; } = new();

    [ViewVariables]
    public HashSet<Enum> UserInterfaces { get; set; } = new();
}
