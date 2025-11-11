using Content.Shared.Body.Prototypes;
using Content.Shared.Surgery.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Surgery.UI;

public sealed partial class SurgeryOrganButton : BaseSurgeryReceiverButton
{
    public SurgeryOrganButton(ISurgeryReceiver receiver, ProtoId<OrganPrototype> organId, IPrototypeManager prototype, SpriteSystem sprite) : base(receiver)
    {
        var organ = prototype.Index(organId);

        Label.Text = Loc.GetString(organ.Name);
        Icon.Texture = sprite.Frame0(organ.EmptySlotSprite);
    }
}
