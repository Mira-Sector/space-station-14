using Content.Shared.Body.Organ;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Body;

[UsedImplicitly]
public sealed class OrganSelectionBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private OrganSelectionWindow? _window;

    public OrganSelectionBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<OrganSelectionWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not OrganSelectionBoundUserInterfaceState cast)
            return;

        _window.UpdateState(cast);
    }
}
