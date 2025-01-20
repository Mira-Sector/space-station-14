using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery;

[Prototype]
public sealed partial class SurgeryPrototype : SurgeryGraph, IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;
}
