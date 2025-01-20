using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery;

[Prototype]
public sealed partial class SurgeryPrototype : SurgeryGraph, IPrototype
{
    public const string StartingNode = "start";

    [IdDataField]
    public string ID { get; } = default!;
}
