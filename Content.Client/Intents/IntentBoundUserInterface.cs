using Content.Client.UserInterface.Controls;
using Content.Shared.Intents;
using Content.Shared.Intents.Events;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Intents;

[UsedImplicitly]
public sealed class IntentBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private SimpleRadialMenu? _menu;

    private static readonly SimpleRadialMenuSettings Settings = new()
    {
        DefaultContainerRadius = 75,
    };

    public IntentBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<IntentsComponent>(Owner, out var intents))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        var models = ConvertToButtons(intents.SelectableIntents);
        _menu.SetButtons(models, Settings);

        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuActionOption> ConvertToButtons(HashSet<ProtoId<IntentPrototype>> intents)
    {
        foreach (var intentId in intents)
        {
            if (!_prototype.TryIndex(intentId, out var intent))
                continue;

            yield return new RadialMenuActionOption<ProtoId<IntentPrototype>>(HandleButtonClick, intentId)
            {
                Sprite = intent.Icon,
                ToolTip = Loc.GetString(intent.Name),
                BackgroundColor = intent.BackgroundColor,
                HoverBackgroundColor = intent.HoverColor
            };
        }
    }

    private void HandleButtonClick(ProtoId<IntentPrototype> intent)
    {
        var msg = new IntentChangeMessage(intent);
        SendPredictedMessage(msg);
    }
}
