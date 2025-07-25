using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class SurgerySpecial
{
    public virtual void NodeReached(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui)
    {
        ui = null;
    }

    public virtual void NodeLeft(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui)
    {
        ui = null;
    }

    public virtual SurgeryInteractionState Interacted(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui)
    {
        ui = null;
        return SurgeryInteractionState.Failed;
    }

    public virtual bool StartDoAfter(SharedDoAfterSystem doAfter, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, [NotNullWhen(true)] out DoAfterId? doAfterId)
    {
        doAfterId = null;
        return false;
    }
}
