using JetBrains.Annotations;

namespace Content.Shared.Surgery;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class SurgeryEdgeRequirement
{
    public abstract bool RequirementMet(EntityUid body, EntityUid limb, EntityUid user, EntityUid tool);
}
