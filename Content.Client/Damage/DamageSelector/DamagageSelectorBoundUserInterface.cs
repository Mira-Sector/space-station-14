using Content.Shared.Body.Part;
using Content.Shared.Damage.DamageSelector;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;

namespace Content.Client.Damage.DamageSelector;

[UsedImplicitly]
public sealed class DamageSelectorBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private DamageSelectorMenu? _menu;

    public DamageSelectorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<DamageSelectorMenu>();
        _menu.SetEntity(Owner);
        _menu.SendDamageSelectorMessageAction += SendDamageSelectorSystemMessage;

        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }

    public void SendDamageSelectorSystemMessage(BodyPart part)
    {
        SendMessage(new DamageSelectorSystemMessage(part));
    }
}
