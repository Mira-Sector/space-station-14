using Content.Shared.Telescience.Components;
using Robust.Client.UserInterface;


namespace Content.Client.Telescience.Ui;

public sealed class TeleframeConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private TeleframeConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<TeleframeConsoleWindow>();

        if (!EntMan.TryGetComponent<TeleframeConsoleComponent>(Owner, out var teleComp))
            return;

        var xform = EntMan.GetComponent<TransformComponent>(Owner);

        _window.UpdateState((Owner, teleComp, xform));

        _window.OnActivated += SendPredictedMessage;
    }
}
