using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Surgery.Pain;

[Prototype]
public sealed partial class SurgeryPainPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <inheritdoc/>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<SurgeryPainPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc>
    [AbstractDataField]
    [NeverPushInheritance]
    public bool Abstract { get; private set; }

    [DataField]
    [AlwaysPushInheritance]
    public List<SurgeryPainRequirement> Requirements = [];

    [DataField]
    [AlwaysPushInheritance]
    public List<SurgeryPainEffect> Effects = [];
}
