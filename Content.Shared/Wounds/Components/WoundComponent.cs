using Content.Shared.Surgery.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Wounds.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class WoundComponent : Component
{
    [DataField(required: true, customTypeSerializer:typeof(PrototypeIdSerializer<SurgeryPrototype>))]
    public string Graph { get; set; } = string.Empty;

    [DataField(required: true)]
    public string Node { get; set; } = default!;

    [DataField]
    public int? EdgeIndex { get; set; } = null;

    [DataField]
    public int StepIndex { get; set; } = 0;

    [DataField]
    public string? TargetNode { get; set; } = null;

    [ViewVariables]
    public int? TargetEdgeIndex { get; set; } = null;

    [ViewVariables]
    public Queue<string>? NodePathfinding { get; set; } = null;

    [ViewVariables]
    public readonly Queue<(EntityUid, object)> InteractionQueue = new();
}
