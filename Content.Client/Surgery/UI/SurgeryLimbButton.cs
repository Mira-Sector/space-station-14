using Content.Shared.Body.Part;
using Content.Shared.Surgery.Components;
using Content.Shared.Surgery.Systems;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Surgery.UI;

public sealed partial class SurgeryLimbButton : BaseSurgeryReceiverButton
{
    private static readonly ResPath IconRsi = new("/Textures/Interface/Actions/zone_sel.rsi");

    public SurgeryLimbButton(ISurgeryReceiver receiver, BodyPart bodyPart, SpriteSystem sprite) : base(receiver)
    {
        Label.Text = Loc.GetString(SurgeryHelper.GetBodyPartLoc(bodyPart));
        Icon.Texture = sprite.Frame0(new SpriteSpecifier.Rsi(IconRsi, SurgeryHelper.BodyPartIconState(bodyPart)));
    }
}
