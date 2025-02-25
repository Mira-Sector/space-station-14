using Content.Shared.Body.Organ;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Body;

[UsedImplicitly]
public sealed class OrganSelectionBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private OrganSelectionWindow? _window;

    [ViewVariables]
    private EntityUid? _owner;

    public OrganSelectionBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _owner = owner;
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<OrganSelectionWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || _owner == null || state is not OrganSelectionBoundUserInterfaceState cast)
            return;

        _window.UpdateState(_owner.Value, cast);

        foreach (var (button, netId) in _window.Buttons)
        {
            button.OnOrganPressed += (organ) =>
            {
                SendMessage(new OrganSelectionButtonPressedMessage(organ, netId));
            };
        }
    }
}
