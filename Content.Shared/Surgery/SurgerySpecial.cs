using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Content.Shared.Surgery.Events;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Surgery;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class SurgerySpecial
{
    public virtual void NodeReached(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui, out bool bodyUi)
    {
        ui = null;
        bodyUi = false;
    }

    public virtual void NodeLeft(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui, out bool bodyUi)
    {
        ui = null;
        bodyUi = false;
    }

    public virtual SurgeryInteractionState Interacted(SurgerySpecialInteractionPhase phase, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui, out bool bodyUi)
    {
        ui = null;
        bodyUi = false;
        return SurgeryInteractionState.Failed;
    }

    public virtual bool StartDoAfter(SharedDoAfterSystem doAfter, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, [NotNullWhen(true)] out DoAfterId? doAfterId)
    {
        doAfterId = null;
        return false;
    }

    public virtual void OnDoAfter(EntityUid? body, EntityUid? limb, SurgerySpecialDoAfterEvent args)
    {
    }

    #region UI

    public abstract string Name(EntityUid? body, EntityUid? limb, BodyPart bodyPart);
    public abstract string Description(EntityUid? body, EntityUid? limb, BodyPart bodyPart);
    public abstract SpriteSpecifier? GetIcon(EntityUid? body, EntityUid? limb, BodyPart bodyPart);

    #endregion
}
