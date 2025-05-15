namespace Content.Server.Supermatter.Delaminations;

[ImplicitDataDefinitionForInheritors]
public abstract partial class DelaminationRequirement
{
    public abstract bool RequirementMet(EntityUid supermatter, IEntityManager entMan);
}
