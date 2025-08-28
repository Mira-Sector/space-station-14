using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class ChatUi : UIFragment
{
    private ChatUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new ChatUiFragment(userInterface, fragmentOwner!.Value);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is ChatUiState cast)
            _fragment?.UpdateState(cast.Messages);
    }
}
