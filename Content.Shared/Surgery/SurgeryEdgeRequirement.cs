namespace Content.Shared.Surgery;

[ImplicitDataDefinitionForInheritors]
public abstract partial class SurgeryEdgeRequirement
{
    public abstract bool RequirementMet(EntityUid mob);
}
