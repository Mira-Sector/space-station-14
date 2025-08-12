using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Content.Shared.Surgery.Pain;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Surgery;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class SurgeryEdgeRequirement
{
    [DataField]
    public HashSet<ProtoId<SurgeryPainPrototype>> Pain = [];

    public abstract SurgeryInteractionState RequirementMet(EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, out Enum? ui, bool test = false);

    public virtual bool StartDoAfter(SharedDoAfterSystem doAfter, SurgeryEdge targetEdge, EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, [NotNullWhen(true)] out DoAfterId? doAfterId)
    {
        doAfterId = null;
        return false;
    }

    public abstract bool RequirementsMatch(SurgeryEdgeRequirement other, [NotNullWhen(true)] out SurgeryEdgeRequirement? merged);

    #region UI

    public abstract string Name(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart bodyPart);
    public abstract string Description(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart bodyPart);
    public abstract SpriteSpecifier? GetIcon(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart bodyPart);

    #endregion
}
