using Content.Shared.Modules.ModSuit;
using Robust.Client.UserInterface;
using JetBrains.Annotations;

namespace Content.Client.Modules.ModSuit;

[UsedImplicitly]
public sealed partial class ModSuitBoundUserInterface : BoundUserInterface
{
    private ModSuitWindow? _window;

    public ModSuitBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ModSuitWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case ModSuitSealableBoundUserInterfaceState sealable:
            {
                _window?.UpdateSealed(sealable);
                break;
            }
        }
    }
}
