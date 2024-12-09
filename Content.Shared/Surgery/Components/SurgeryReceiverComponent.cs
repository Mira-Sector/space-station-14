using Content.Shared.Surgery.Prototypes;
using Content.Shared.Surgery.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Surgery.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SurgerySystem))]
public sealed partial class SurgeryReceiverComponent : Component
{
    [DataField(customTypeSerializer:typeof(PrototypeIdSerializer<SurgeryPrototype>))]
    public List<string> AvailableSurgeries { get; set; } = new();

    [ViewVariables]
    public SurgeryGraph Graph { get; set; } = default!;

    [ViewVariables]
    public string? Node { get; set; } = default!;

    [ViewVariables]
    public int? EdgeIndex { get; set; } = null;

    [ViewVariables]
    public int StepIndex { get; set; } = 0;

    [ViewVariables]
    public string? TargetNode { get; set; } = null;

    [ViewVariables]
    public int? TargetEdgeIndex { get; set; } = null;

    [ViewVariables]
    public Queue<string>? NodePathfinding { get; set; } = null;

    [ViewVariables]
    public readonly Queue<(EntityUid, object)> InteractionQueue = new();
}
