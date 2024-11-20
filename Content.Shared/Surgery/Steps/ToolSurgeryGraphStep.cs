using Content.Shared.Tools;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Surgery.Steps;

[DataDefinition]
public sealed partial class ToolSurgeryGraphStep : SurgeryGraphStep
{
    [DataField(required:true, customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    public string Tool { get; private set; } = string.Empty;

    [DataField]
    public float Fuel { get; private set; } = 10;
}

