using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class PowerTwoUi : UIFragment
{
    private PowerTwoUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new PowerTwoUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not PowerTwoUiState cast)
            return;

        _fragment?.UpdateState(cast.Grid, cast.GridSize, cast.MaxValue);
    }
}
