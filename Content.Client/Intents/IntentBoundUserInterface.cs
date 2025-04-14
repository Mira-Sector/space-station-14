using Content.Client.UserInterface.Controls;
using Content.Shared.Intents;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Intents;

[UsedImplicitly]
public sealed class IntentBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IntentSystem _intent = default!;

    private SimpleRadialMenu? _menu;

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
        _menu.Track(Owner);
        var models = ConvertToButtons(intents.SelectableIntents, intents);
        _menu.SetButtons(models);

        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuActionOption> ConvertToButtons(HashSet<Intent> intents, IntentsComponent intentComp)
    {
        foreach (var intent in intents)
        {
            yield return new RadialMenuActionOption<Intent>(HandleButtonClick, intent)
            {
                Sprite = _intent.GetIntentIcon((Owner, intentComp), intent),
                ToolTip = _intent.GetIntentName(intent)
            };
        }
    }

    private void HandleButtonClick(Intent intent)
    {
        var msg = new IntentChangeMessage(intent);
        SendPredictedMessage(msg);
    }
}
