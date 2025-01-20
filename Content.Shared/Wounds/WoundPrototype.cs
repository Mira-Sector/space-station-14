using Robust.Shared.Prototypes;

namespace Content.Shared.Wounds.Prototypes;

[Prototype]
public sealed partial class WoundPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true)]
    public ComponentRegistry Components { get; private set; } = new();

    /// <summary>
    /// If this is true then existing components will be removed and replaced with these ones.
    /// </summary>
    [DataField]
    public bool RemoveExisting = true;
}
