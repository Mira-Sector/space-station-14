using Content.Shared.Intents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Intents;

[Prototype]
public sealed partial class IntentPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Name to show in the menu and action
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public required SpriteSpecifier Icon;

    /// <summary>
    /// Event to raise when the user selects this intent in the menu
    /// </summary>
    [DataField(required: true)]
    public required BaseIntentEvent ActivatedEvent;

    /// <summary>
    /// Event to raise when the user has selected a new intent that replaces this one
    /// </summary>
    [DataField(required: true)]
    public required BaseIntentEvent DeactivatedEvent;

    [DataField]
    public Color? BackgroundColor;

    [DataField]
    public Color? HoverColor;
}
