using Content.Shared.Body.Part;
using Content.Shared.Surgery;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Surgery.UI;

[GenerateTypedNameReferences]
public sealed partial class SurgeryEdgeDetailsTab : PanelContainer
{
    public SurgeryEdgeDetailsTab(SurgeryEdgeRequirement requirement, EntityUid? body, EntityUid? limb, BodyPart part, SpriteSystem sprite) : base()
    {
        RobustXamlLoader.Load(this);

        Name = requirement.Name(body, limb, part);

        var icon = requirement.GetIcon(body, limb, part);
        RequirementIcon.Texture = icon == null ? null : sprite.Frame0(icon);
        RequirementDescription.Text = requirement.Description(body, limb, part);
    }
}
