using Content.Shared.Arcade.Racer.Components;
using Robust.Client.UserInterface;

namespace Content.Client.Arcade.Racer;

public sealed partial class RacerGameBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private RacerGameWindow? _window;

    public RacerGameBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (PlayerManager.LocalEntity is not { } viewer)
            return;

        if (!EntMan.TryGetComponent<RacerArcadeComponent>(Owner, out var racer))
            return;

        _window = this.CreateWindow<RacerGameWindow>();
        _window.SetCabinet((Owner, racer), viewer);
    }
}
