namespace Content.Client.CartridgeLoader.Cartridges;

public interface IChatUiFragmentMode
{
    Action<BaseChatUiFragmentPopup>? OnPopupAdd { get; set; }
}
