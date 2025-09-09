using Robust.Shared.Prototypes;

namespace Content.Shared.Holodeck;

[Prototype]
public sealed partial class HolodeckScenarioPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField]
    public List<Box2i>? RequiredSpace;
}
