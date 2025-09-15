using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.PDA.Messaging.Recipients;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class ChatUi : UIFragment
{
    private ChatUiFragment? _fragment;

    private ChatUiMode _uiMode;
    private BasePdaChatMessageable? _recipient = null;

    private ChatUiState? _lastState = null;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new ChatUiFragment(fragmentOwner!.Value);

        if (_lastState != null)
            SetState(_lastState);

        _fragment.ChangeRecipient(_recipient);
        _fragment.ChangeMode(_uiMode);
        _fragment.OnRecipientChanged += recipient => _recipient = recipient;
        _fragment.OnModeChanged += mode => _uiMode = mode;
        _fragment.OnPayloadSend += payload =>
        {
            var message = new ChatUiMessageEvent(payload);
            var cartridgeMessage = new CartridgeUiMessage(message);
            userInterface.SendPredictedMessage(cartridgeMessage);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not ChatUiState cast)
            return;

        SetState(cast);
        _lastState = cast;
    }

    private void SetState(ChatUiState state)
    {
        _fragment?.UpdateState(state);
    }
}
