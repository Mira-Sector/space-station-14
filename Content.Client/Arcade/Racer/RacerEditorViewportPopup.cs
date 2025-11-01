using System.Numerics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Arcade.Racer;

[Virtual]
public partial class RacerEditorViewportPopup : Popup
{
    private RacerEditorViewportPopup? _popup = null;

    public RacerEditorViewportPopup() : base()
    {
        CloseOnClick = false;
        MouseFilter = MouseFilterMode.Stop;
    }

    protected void OpenPopup(RacerEditorViewportPopup popup)
    {
        if (_popup is { } oldPopup)
        {
            oldPopup.Close();
            RemoveChild(oldPopup);
        }

        popup.OnPopupHide += () => _popup = null;

        var box = UIBox2.FromDimensions(UserInterfaceManager.MousePositionScaled.Position, Vector2.One);
        popup.Open(box);
        AddChild(popup);
        _popup = popup;
    }
}
