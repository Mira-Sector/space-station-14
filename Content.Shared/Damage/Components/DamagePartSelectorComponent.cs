using Content.Shared.Body.Part;

namespace Content.Shared.Damage.Components;

[RegisterComponent]
public sealed partial class DamagePartSelectorComponent : Component
{
    [ViewVariables]
    public BodyPart SelectedPart = new BodyPart(BodyPartType.Torso, BodyPartSymmetry.None);
}
