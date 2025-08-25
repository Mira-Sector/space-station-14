using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
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

        _fragment.OnDragged += dir =>
        {
            var message = new PowerTwoUiMoveMessageEvent(dir);
            SendUiMessage(message, userInterface);
        };

        _fragment.OnNewGame += () =>
        {
            var message = new PowerTwoUiNewGameMessageEvent();
            SendUiMessage(message, userInterface);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not PowerTwoUiState cast)
            return;

        _fragment?.UpdateState(cast.GameState, cast.Grid, cast.GridSize, cast.MaxValue, cast.StartTime);
    }

    private static void SendUiMessage(CartridgeMessageEvent message, BoundUserInterface userInterface)
    {
        var cartridgeMessage = new CartridgeUiMessage(message);
        userInterface.SendPredictedMessage(cartridgeMessage);
    }
}
