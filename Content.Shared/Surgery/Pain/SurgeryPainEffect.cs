using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.Pain;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class SurgeryPainEffect
{
    public abstract void DoEffect(IEntityManager entity, EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid? used);
}
