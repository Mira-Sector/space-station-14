using Content.Shared.Body.Part;

namespace Content.Shared.Damage.Components;

[RegisterComponent]
public sealed partial class DamagePartSelectorComponent : Component
{
    [ViewVariables]
    public BodyPartType SelectedPart = BodyPartType.Torso;

    [ViewVariables]
    public BodyPartSymmetry Side = BodyPartSymmetry.None;
}
