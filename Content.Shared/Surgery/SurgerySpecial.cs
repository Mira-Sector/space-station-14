using Content.Shared.Body.Part;
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

    public virtual bool Interacted(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui)
    {
        ui = null;
        return false;
    }
}
