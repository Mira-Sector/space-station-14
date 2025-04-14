using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Intents;

[Prototype]
public sealed partial class IntentPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public required SpriteSpecifier Icon;

    [DataField]
    public Color? BackgroundColor;

    [DataField]
    public Color? HoverColor;
}
