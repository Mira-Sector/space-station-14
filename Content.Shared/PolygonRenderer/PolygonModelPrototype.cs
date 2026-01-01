using Robust.Shared.Prototypes;

namespace Content.Shared.PolygonRenderer;

[Prototype]
public sealed partial class PolygonModelPrototype : PolygonModel, IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;
}
