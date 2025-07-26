using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryReceiverComponent : Component, ISurgeryReceiver
{
    [DataField]
    public List<ProtoId<SurgeryPrototype>> AvailableSurgeries { get; set; } = [];

    [ViewVariables]
    public SurgeryGraph Graph { get; set; } = new();

    [ViewVariables]
    public SurgeryNode? CurrentNode { get; set; }

    [ViewVariables]
    public Dictionary<(NetEntity, ushort), (NetEntity, SurgeryEdgeRequirement)> EdgeDoAfters { get; set; } = [];

    [ViewVariables]
    public Dictionary<SurgerySpecial, Dictionary<NetEntity, ushort>> SpecialDoAfters { get; set; } = [];

    [ViewVariables]
    public HashSet<Enum> UserInterfaces { get; set; } = [];
}
