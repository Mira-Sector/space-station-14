using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Surgery.Requirements;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class OrganRequirement : SurgeryEdgeRequirement
{
    public override string Description(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Loc.GetString("surgery-requirement-organ-desc");
    }

    public override SurgeryEdgeState RequirementMet(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, out Enum? ui)
    {
        ui = null;

        // the body doesnt contain any organs
        if (limb == null)
            return SurgeryEdgeState.Failed;

        ui = OrganSelectionUiKey.Key;
        return SurgeryEdgeState.UserInterface;
    }

    public override bool StartDoAfter(SharedDoAfterSystem doAfter, SurgeryEdge targetEdge, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, [NotNullWhen(true)] out DoAfterId? doAfterId)
    {
        doAfterId = null;
        return false;
    }

    public override bool RequirementsMatch(SurgeryEdgeRequirement other, [NotNullWhen(true)] out SurgeryEdgeRequirement? merged)
    {
        merged = null;

        if (other is not OrganRequirement otherOrgan)
            return false;

        merged = new OrganRequirement();
        return true;
    }
}
