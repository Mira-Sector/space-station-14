using Content.Shared.Body.Part;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class SurgerySpecial
{
    public abstract void NodeReached(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart);

    public abstract void NodeLeft(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart);
}
