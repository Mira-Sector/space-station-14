using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.Pain;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class SurgeryPainRequirement
{
    public abstract bool RequirementMet(IEntityManager entity, EntityUid? body, EntityUid? limb, EntityUid? used);
}
