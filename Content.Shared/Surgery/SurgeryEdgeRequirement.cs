using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Surgery;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class SurgeryEdgeRequirement
{
    public abstract SurgeryInteractionState RequirementMet(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, out Enum? ui);

    public virtual bool StartDoAfter(SharedDoAfterSystem doAfter, SurgeryEdge targetEdge, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, [NotNullWhen(true)] out DoAfterId? doAfterId)
    {
        doAfterId = null;
        return false;
    }

    public abstract bool RequirementsMatch(SurgeryEdgeRequirement other, [NotNullWhen(true)] out SurgeryEdgeRequirement? merged);

    #region UI

    public abstract string Name(EntityUid? body, EntityUid? limb, BodyPart bodyPart);
    public abstract string Description(EntityUid? body, EntityUid? limb, BodyPart bodyPart);
    public abstract SpriteSpecifier? GetIcon(EntityUid? body, EntityUid? limb, BodyPart bodyPart);

    #endregion
}
