namespace Content.Client.CartridgeLoader.Cartridges;

public interface IChatUiFragmentMode
{
    event Action<BaseChatUiFragmentPopup>? OnPopupAdd;
}
