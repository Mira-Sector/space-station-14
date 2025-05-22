using Content.Client.PolygonRenderer;
using Robust.Client.UserInterface;

namespace Content.Client.Arcade.UI;

public sealed class RaceGameBoundUserInterface : BoundUserInterface
{
    private BoxTestWindow? _menu;

    public RaceGameBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<BoxTestWindow>();
    }
}
