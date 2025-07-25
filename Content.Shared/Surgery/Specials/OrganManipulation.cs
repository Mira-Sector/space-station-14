using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.Specials;

[UsedImplicitly, Serializable, NetSerializable]
public sealed partial class OrganManipulation : SurgerySpecial
{
    public override bool Interacted(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui)
    {
        base.Interacted(body, limb, user, used, bodyPart, out ui);

        if (used != null)
            return false;

        ui = OrganSelectionUiKey.Key;
        return true;
    }
}
